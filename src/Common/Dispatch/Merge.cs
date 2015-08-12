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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;

namespace NanoByte.Common.Dispatch
{
    /// <summary>
    /// Provides utility methods for merging <see cref="ICollection{T}"/>s.
    /// </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static class Merge
    {
        /// <summary>
        /// Performs a 2-way merge on two collections. Changes required to <paramref name="theirs"/> to reflect <paramref name="mine"/> are emitted using callback delegates.
        /// </summary>
        /// <param name="theirs">The foreign list with changes that shall be merged in.</param>
        /// <param name="mine">The local list that shall be updated with foreign changes.</param>
        /// <param name="added">Called for every element that should be added to <paramref name="mine"/>.</param>
        /// <param name="removed">Called for every element that should be removed from <paramref name="mine"/>.</param>
        /// <remarks><paramref name="theirs"/> and <paramref name="mine"/> should use an internal hashmap for <see cref="ICollection{T}.Contains"/> for better performance.</remarks>
        public static void TwoWay<T>([NotNull, ItemNotNull, InstantHandle] IEnumerable<T> theirs, [NotNull, ItemNotNull, InstantHandle] IEnumerable<T> mine, [NotNull, InstantHandle] Action<T> added, [NotNull, InstantHandle] Action<T> removed)
        {
            #region Sanity checks
            if (theirs == null) throw new ArgumentNullException(nameof(theirs));
            if (mine == null) throw new ArgumentNullException(nameof(mine));
            if (added == null) throw new ArgumentNullException(nameof(added));
            if (removed == null) throw new ArgumentNullException(nameof(removed));
            #endregion

            // Entry in mine, but not in theirs
            foreach (var mineElement in mine.Where(mineElement => !theirs.Contains(mineElement)))
                removed(mineElement);

            // Entry in theirs, but not in mine
            foreach (var theirsElement in theirs.Where(theirsElement => !mine.Contains(theirsElement)))
                added(theirsElement);
        }

        /// <summary>
        /// Performs a 2-way merge on two collections. Changes required to <paramref name="theirs"/> to reflect <paramref name="mine"/> are recorded using differential lists.
        /// </summary>
        /// <param name="theirs">The foreign list with changes that shall be merged in.</param>
        /// <param name="mine">The local list that shall be updated with foreign changes.</param>
        /// <param name="added">All elements that should be added to <paramref name="mine"/> are added to this list.</param>
        /// <param name="removed">All elements that should be removed from <paramref name="mine"/> are added to this list.</param>
        /// <remarks><paramref name="theirs"/> and <paramref name="mine"/> should use an internal hashmap for <see cref="ICollection{T}.Contains"/> for better performance.</remarks>
        public static void TwoWay<T, TAdded, TRemoved>([NotNull, ItemNotNull, InstantHandle] IEnumerable<T> theirs, [NotNull, ItemNotNull, InstantHandle] IEnumerable<T> mine, [NotNull] ICollection<TAdded> added, [NotNull] ICollection<TRemoved> removed)
            where T : class, TAdded, TRemoved
        {
#if __MonoCS__
            TwoWay(theirs, mine, x => added.Add(x), x => removed.Add(x));
#else
            TwoWay(theirs, mine, added.Add, removed.Add);
#endif
        }

        /// <summary>
        /// Performs a 3-way merge on a set of collections. Changes between <paramref name="reference"/> and <paramref name="theirs"/> as they apply to <paramref name="mine"/> are emitted using callback delegates.
        /// </summary>
        /// <param name="reference">A common baseline from which both <paramref name="theirs"/> and <paramref name="mine"/> were modified.</param>
        /// <param name="theirs">The foreign list with changes that shall be merged in.</param>
        /// <param name="mine">The local list that shall be updated with foreign changes.</param>
        /// <param name="added">Called for every element that should be added to <paramref name="mine"/>.</param>
        /// <param name="removed">Called for every element that should be removed from <paramref name="mine"/>.</param>
        /// <remarks>Modified elements are handled by calling <paramref name="removed"/> for the old state and <paramref name="added"/> for the new state.</remarks>
        public static void ThreeWay<T>([NotNull, ItemNotNull, InstantHandle] IEnumerable<T> reference, [NotNull, ItemNotNull, InstantHandle] IEnumerable<T> theirs, [NotNull, ItemNotNull, InstantHandle] IEnumerable<T> mine, [NotNull, InstantHandle] Action<T> added, [NotNull, InstantHandle] Action<T> removed)
            where T : class, IMergeable<T>
        {
            #region Sanity checks
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            if (theirs == null) throw new ArgumentNullException(nameof(theirs));
            if (mine == null) throw new ArgumentNullException(nameof(mine));
            if (added == null) throw new ArgumentNullException(nameof(added));
            if (removed == null) throw new ArgumentNullException(nameof(removed));
            #endregion

            // Entry in theirsList, but not in mineList
            foreach (var theirsElement in theirs.Where(theirsElement => FindMergeID(mine, theirsElement.MergeID) == null && !reference.Contains(theirsElement)))
                added(theirsElement); // Added in theirsList

            foreach (var mineElement in mine)
            {
                var matchingTheirs = FindMergeID(theirs, mineElement.MergeID);
                if (matchingTheirs == null)
                { // Entry in mineList, but not in theirsList
                    if (reference.Contains(mineElement))
                        removed(mineElement); // Removed from theirsList
                }
                else
                { // Entry both in mineList and in theirsList
                    if (!mineElement.Equals(matchingTheirs) && matchingTheirs.Timestamp > mineElement.Timestamp)
                    { // Theirs newer than mine
                        removed(mineElement);
                        added(matchingTheirs);
                    }
                }
            }
        }

        /// <summary>
        /// Performs a 3-way merge on a set of collections. Changes between <paramref name="reference"/> and <paramref name="theirs"/> as they apply to <paramref name="mine"/> are recorded using differential lists.
        /// </summary>
        /// <param name="reference">A common baseline from which both <paramref name="theirs"/> and <paramref name="mine"/> were modified.</param>
        /// <param name="theirs">The foreign list with changes that shall be merged in.</param>
        /// <param name="mine">The local list that shall be updated with foreign changes.</param>
        /// <param name="added">All elements that should be added to <paramref name="mine"/> are added to this list.</param>
        /// <param name="removed">All elements that should be removed from <paramref name="mine"/> are added to this list.</param>
        /// <remarks>Modified elements are handled by adding to <paramref name="removed"/> for the old state and to <paramref name="added"/> for the new state.</remarks>
        public static void ThreeWay<T, TAdded, TRemoved>([NotNull, ItemNotNull, InstantHandle] IEnumerable<T> reference, [NotNull, ItemNotNull, InstantHandle] IEnumerable<T> theirs, [NotNull, ItemNotNull, InstantHandle] IEnumerable<T> mine, [NotNull] ICollection<TAdded> added, [NotNull] ICollection<TRemoved> removed)
            where T : class, IMergeable<T>, TAdded, TRemoved
        {
#if __MonoCS__
            ThreeWay(reference, theirs, mine, x => added.Add(x), x => removed.Add(x));
#else
            ThreeWay(reference, theirs, mine, added.Add, removed.Add);
#endif
        }

        /// <summary>
        /// Finds the first element in a list matching the specified <see cref="IMergeable{T}.MergeID"/>.
        /// </summary>
        private static T FindMergeID<T>(IEnumerable<T> elements, string id)
            where T : class, IMergeable<T>
        {
            return elements.FirstOrDefault(element => element != null && element.MergeID == id);
        }
    }
}
