﻿/*
 * Copyright 2006-2015 Bastian Eicher
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using JetBrains.Annotations;
using NanoByte.Common.Streams;

namespace NanoByte.Common.Net
{
    /// <summary>
    /// Provides a minimalistic HTTP webserver that can provide only a single file. Useful for testing download code.
    /// </summary>
    public sealed class MicroServer : IDisposable
    {
        #region Constants
        /// <summary>The lowest port the server tries listening on.</summary>
        private const int MinimumPort = 50222;

        /// <summary>The highest port the server tries listening on.</summary>
        private const int MaxmimumPort = 50734;
        #endregion

        /// <summary>
        /// The URL under which the server root can be reached. Usually you should use <see cref="FileUri"/> instead.
        /// </summary>
        [NotNull]
        public Uri ServerUri { get; }

        /// <summary>
        /// The complete URL under which the server provides its file.
        /// </summary>
        [NotNull]
        public Uri FileUri { get; private set; }

        /// <summary>
        /// The content of the file to be served under <see cref="FileUri"/>.
        /// </summary>
        [NotNull]
        public Stream FileContent { get; private set; }

        /// <summary>
        /// Wait for twenty seconds every time before finishing a response.
        /// </summary>
        public bool Slow { get; set; }

        private readonly string _resourceName;

        /// <summary>
        /// Starts a HTTP webserver that listens on a random port.
        /// </summary>
        /// <param name="resourceName">The HTTP resource name under which to provide the content.</param>
        /// <param name="fileContent">The content of the file to serve.</param>
        [SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings")]
        public MicroServer([NotNull, Localizable(false)] string resourceName, [NotNull] Stream fileContent)
        {
            _resourceName = resourceName;
            FileContent = fileContent;

            ServerUri = new Uri(StartListening());
            FileUri = new Uri(ServerUri, resourceName);

            ThreadUtils.StartBackground(ListenLoop, name: "MicroServer.Listen");
        }

        private HttpListener _listener;

        /// <summary>
        /// Starts listening and returns the URL prefix under which the content is reachable.
        /// </summary>
        private string StartListening()
        {
            int port = MinimumPort;

            // Keep incremeting port number until we find a free one
            while (true)
            {
                try
                {
                    string prefix = "http://localhost:" + port++ + "/";
                    _listener = new HttpListener();
                    _listener.Prefixes.Add(prefix);
                    _listener.Start();
                    return prefix;
                }
                catch (HttpListenerException)
                {
                    if (port > MaxmimumPort) throw;
                }
                catch (SocketException)
                {
                    if (port > MaxmimumPort) throw;
                }
            }
        }

        /// <summary>
        /// Stops listening for incoming HTTP connections.
        /// </summary>
        public void Dispose()
        {
            _listener.Close();
        }

        /// <summary>
        /// Waits for HTTP requests and responds to them if they ask for "file".
        /// </summary>
        private void ListenLoop()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    HandleRequest(context);
                    context.Response.OutputStream.Close();
                }
                    #region Error handling
                catch (HttpListenerException)
                {
                    return;
                }
                catch (InvalidOperationException)
                {
                    return;
                }
                #endregion
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            // Only return one specific file
            if (context.Request.RawUrl != "/" + _resourceName)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            // Delay finishing the file transfer if Slow-mode is active
            if (Slow) Thread.Sleep(20000);

            switch (context.Request.HttpMethod)
            {
                case "GET":
                    context.Response.ContentLength64 = FileContent.Length;
                    FileContent.CopyToEx(context.Response.OutputStream);
                    break;

                case "PUT":
                    FileContent = new MemoryStream();
                    context.Request.InputStream.CopyToEx(FileContent);
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    break;
            }
        }
    }
}
