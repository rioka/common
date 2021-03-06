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

using FluentAssertions;
using NUnit.Framework;

namespace NanoByte.Common.Collections
{
    /// <summary>
    /// Contains test methods for <see cref="ArrayExtensions"/>.
    /// </summary>
    [TestFixture]
    public class ArrayExtensionsTest
    {
        [Test]
        public void TestAppend()
        {
            var strings = new[] {"A", "B"};
            strings.Append("C").Should().Equal("A", "B", "C");
        }

        [Test]
        public void TestPrepend()
        {
            var strings = new[] {"B", "C"};
            strings.Prepend("A").Should().Equal("A", "B", "C");
        }

        [Test]
        public void TestGetAddedElements()
        {
            new[] {"A", "B", "C", "E", "G", "H"}.GetAddedElements(new[] {"A", "C", "E", "G"}).Should().Equal("B", "H");
            new[] {"C", "D"}.GetAddedElements(new[] {"A", "D"}).Should().Equal("C");
        }
    }
}
