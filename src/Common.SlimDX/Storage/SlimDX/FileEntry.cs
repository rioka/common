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
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using JetBrains.Annotations;
using NanoByte.Common.Controls;
using NanoByte.Common.Properties;

namespace NanoByte.Common.Storage.SlimDX
{

    #region Enumerations
    /// <seealso cref="FileEntry.EntryType"/>
    public enum FileEntryType
    {
        /// <summary>The file is present in the main game and was not modified by a mod.</summary>
        Normal,

        /// <summary>The file is present in the main game and was modified/overwritten by a mod.</summary>
        Modified,

        /// <summary>The file is not present in the main game and was added by a mod.</summary>
        Added,

        /// <summary>The file was originally added by a mod but has now been deleted.</summary>
        Deleted
    }
    #endregion

    /// <summary>
    /// Describes a file returned by <see cref="ContentManager.GetFileList"/>.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes", Justification = "Comparison only used for INamed sorting")]
    public sealed class FileEntry : INamed<FileEntry>, IHighlightColor, IContextMenu, IEquatable<FileEntry>
    {
        #region Properties
        /// <summary>
        /// The type of file (e.g. Textures, Sounds, ...).
        /// </summary>
        /// <remarks>This is only used for file operations and not for sorting!</remarks>
        public string FileType { get; }

        private readonly string _name;

        /// <summary>
        /// The relative file path.
        /// </summary>
        public string Name { get { return _name; } set { throw new NotSupportedException(); } }

        /// <summary>
        /// The kind of file entry this is (in relation to its mod status).
        /// </summary>
        public FileEntryType EntryType { get; set; }

        /// <summary>
        /// The color to highlight this file entry with in list representations.
        /// <see cref="Color.Empty"/> for <see cref="FileEntryType.Normal"/> (no highlighting).
        /// <see cref="Color.Blue"/> for <see cref="FileEntryType.Modified"/>.
        /// <see cref="Color.Green"/> for <see cref="FileEntryType.Added"/>.
        /// <see cref="Color.Red"/> for <see cref="FileEntryType.Deleted"/>.
        /// </summary>
        public Color HighlightColor
        {
            get
            {
                switch (EntryType)
                {
                    case FileEntryType.Normal:
                        return Color.Empty;
                    case FileEntryType.Modified:
                        return Color.Blue;
                    case FileEntryType.Added:
                        return Color.Green;
                    case FileEntryType.Deleted:
                        return Color.Red;
                    default:
                        throw new InvalidOperationException(); // Can never be reached
                }
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new file entry.
        /// </summary>
        /// <param name="type">The type of file (e.g. Textures, Sounds, ...).</param>
        /// <param name="name">The relative file path.</param>
        /// <param name="entryType">The kind of file entry this is (in relation to its mod status).</param>
        internal FileEntry([NotNull] string type, [NotNull, Localizable(false)] string name, FileEntryType entryType = FileEntryType.Normal)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            #endregion

            _name = name;
            FileType = type;
            EntryType = entryType;
        }
        #endregion

        //--------------------//

        #region Context menu
        /// <summary>
        /// Returns the context menu for this file entry; can be <c>null</c>.
        /// </summary>
        public ContextMenu GetContextMenu()
        {
            if (EntryType == FileEntryType.Normal) return null;

            // Create context menu to revert mod changes
            var revertMenuEntry = new MenuItem(EntryType == FileEntryType.Added ? Resources.Delete : Resources.Revert);
            revertMenuEntry.Click += delegate
            {
                // Prevent multiple calls
                if (EntryType == FileEntryType.Normal || EntryType == FileEntryType.Deleted) return;

                if (!Msg.YesNo(null, Resources.LoseChangesAsk, MsgSeverity.Warn)) return;

                try
                {
                    ContentManager.DeleteModFile(FileType, _name);
                }
                    #region Error handling
                catch (IOException)
                {
                    Msg.Inform(null, Resources.UnableToDelete, MsgSeverity.Error);
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    Msg.Inform(null, Resources.UnableToDelete, MsgSeverity.Error);
                    return;
                }
                #endregion

                // Update entry status
                if (EntryType == FileEntryType.Added) EntryType = FileEntryType.Deleted;
                if (EntryType == FileEntryType.Modified) EntryType = FileEntryType.Normal;
            };
            return new ContextMenu(new[] {revertMenuEntry});
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(FileEntry other)
        {
            if (ReferenceEquals(null, other)) return false;

            return StringUtils.EqualsIgnoreCase(Name, other.Name);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is FileEntry && Equals((FileEntry)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _name.ToUpperInvariant().GetHashCode();
        }
        #endregion

        #region Comparison
        int IComparable<FileEntry>.CompareTo(FileEntry other)
        {
            return (other == null) ? 0 : string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
        #endregion
    }
}
