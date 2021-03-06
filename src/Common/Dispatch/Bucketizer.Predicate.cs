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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;

namespace NanoByte.Common.Dispatch
{
    /// <summary>
    /// Splits collections into multiple buckets based on predicate matching. The first matching predicate wins. Create with <see cref="Bucketizer.Bucketize{T}"/>.
    /// </summary>
    /// <typeparam name="T">The common base type of all objects to be bucketized.</typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class Bucketizer<T> : IEnumerable<BucketRule<T>>
    {
        private readonly IEnumerable<T> _elements;
        private readonly List<BucketRule<T>> _rules = new List<BucketRule<T>>();

        /// <summary>
        /// Creates a new predicate-matching bucketizer.
        /// </summary>
        /// <param name="elements">The elements to be bucketized.</param>
        internal Bucketizer([NotNull] IEnumerable<T> elements)
        {
            #region Sanity checks
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            #endregion

            _elements = elements;
        }

        /// <summary>
        /// Adds a new bucket rule.
        /// </summary>
        /// <param name="predicate">A condition to check elements against.</param>
        /// <param name="bucket">The collection elements are added to if they match the <paramref name="predicate"/>.</param>
        /// <returns>The "this" pointer for use in a "Fluent API" style.</returns>
        public Bucketizer<T> Add([NotNull] Predicate<T> predicate, [NotNull] ICollection<T> bucket)
        {
            #region Sanity checks
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));
            #endregion

            _rules.Add(new BucketRule<T>(predicate, bucket));

            return this;
        }

        /// <summary>
        /// Adds each element to the first bucket with a matching predicate (if any). Set up with <see cref="Add"/> first.
        /// </summary>
        public void Run()
        {
            foreach (var element in _elements)
            {
                // ReSharper disable once AccessToForEachVariableInClosure
                var matchedRule = _rules.FirstOrDefault(rule => rule.Predicate(element));
                matchedRule?.Bucket.Add(element);
            }
        }

        #region IEnumerable
        public IEnumerator<BucketRule<T>> GetEnumerator()
        {
            return _rules.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _rules.GetEnumerator();
        }
        #endregion
    }

    /// <summary>
    /// Contains extension methods for creating bucketizers.
    /// </summary>
    public static partial class Bucketizer
    {
        /// <summary>
        /// Creates a new predicate-matching bucketizer.
        /// </summary>
        /// <param name="elements">The elements to be bucketized.</param>
        public static Bucketizer<T> Bucketize<T>([NotNull] this IEnumerable<T> elements)
        {
            return new Bucketizer<T>(elements);
        }
    }
}
