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

using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using NanoByte.Common.Storage;
using NUnit.Framework;

namespace NanoByte.Common.Native
{
    /// <summary>
    /// SUMMARY
    /// </summary>
    [TestFixture]
    public class CygwinUtilsTest
    {
        private static readonly byte[] _symlinkBytes = CygwinUtils.SymlinkCookie
            .Concat(Encoding.Unicode.GetPreamble())
            .Concat(Encoding.Unicode.GetBytes("target\0"))
            .ToArray();

        [Test]
        public void TestIsSymlinkNoMatch()
        {
            using (var tempDir = new TemporaryDirectory("unit-tests"))
            {
                string normalFile = Path.Combine(tempDir, "normal");
                FileUtils.Touch(normalFile);

                CygwinUtils.IsSymlink(normalFile).Should().BeFalse();

                string target;
                CygwinUtils.IsSymlink(normalFile, out target).Should().BeFalse();
            }
        }

        [Test]
        public void TestIsSymlinkMatch()
        {
            using (var tempDir = new TemporaryDirectory("unit-tests"))
            {
                string symlinkFile = Path.Combine(tempDir, "symlink");
                File.WriteAllBytes(symlinkFile, _symlinkBytes);
                File.SetAttributes(symlinkFile, FileAttributes.System);

                CygwinUtils.IsSymlink(symlinkFile).Should().BeTrue();

                string target;
                CygwinUtils.IsSymlink(symlinkFile, out target).Should().BeTrue();
                target.Should().Be("target");
            }
        }

        [Test]
        public void TestCreateSymlink()
        {
            if (!WindowsUtils.IsWindows) Assert.Ignore("The 'system' file attribute can only be set on the Windows platform.");

            using (var tempDir = new TemporaryDirectory("unit-tests"))
            {
                string symlinkFile = Path.Combine(tempDir, "symlink");
                CygwinUtils.CreateSymlink(symlinkFile, "target");

                File.Exists(symlinkFile).Should().BeTrue();
                CollectionAssert.AreEqual(expected: _symlinkBytes, actual: File.ReadAllBytes(symlinkFile));
            }
        }
    }
}
