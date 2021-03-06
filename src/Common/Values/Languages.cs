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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using NanoByte.Common.Collections;

namespace NanoByte.Common.Values
{
    /// <summary>
    /// Provides <see cref="CultureInfo"/>s.
    /// </summary>
    public static class Languages
    {
        /// <summary>
        /// All known languages in alphabetical order.
        /// </summary>
        [NotNull]
        public static readonly IEnumerable<CultureInfo> AllKnown = GetAllKnown();

        private static IEnumerable<CultureInfo> GetAllKnown()
        {
            var cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures | CultureTypes.SpecificCultures);
            Array.Sort(cultures, CultureComparer.Instance);
            return cultures.Skip(1);
        }

        /// <summary>
        /// Creates a <see cref="CultureInfo"/> from a ISO language code either in Windows (e.g. en-US) or Unix (e.g. en_US) format.
        /// </summary>
        public static CultureInfo FromString(string langCode)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(langCode)) throw new ArgumentNullException(nameof(langCode));
            #endregion

            return new CultureInfo(langCode.Replace('_', '-'));
        }

        /// <summary>
        /// Changes the UI language used by this process. Should be called right after startup.
        /// </summary>
        /// <remarks>This sets <see cref="CultureInfo.CurrentUICulture"/> for the current and all future threads.</remarks>
        public static void SetUI([NotNull] CultureInfo culture)
        {
            #region Sanity checks
            if (culture == null) throw new ArgumentNullException(nameof(culture));
            #endregion

            var type = typeof(CultureInfo);
            try
            {
                type.InvokeMember("s_userDefaultUICulture", BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static, null, culture, new object[] {culture});
            }
            catch (MissingMemberException)
            {}

            try
            {
                type.InvokeMember("m_userDefaultUICulture", BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static, null, culture, new object[] {culture});
            }
            catch (MissingMemberException)
            {}

            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}
