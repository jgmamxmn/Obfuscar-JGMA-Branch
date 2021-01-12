#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>

/// <copyright>
/// Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>

#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace Obfuscar
{
    static class NameMaker
    {
        static string uniqueChars1;
        static int numUniqueChars;
        const string defaultChars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz";

        const string unicodeChars = "\u00A0\u1680" +
                                    "\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u200B\u2010\u2011\u2012\u2013\u2014\u2015" +
                                    "\u2022\u2024\u2025\u2027\u2028\u2029\u202A\u202B\u202C\u202D\u202E\u202F" +
                                    "\u2032\u2035\u2033\u2036\u203E" +
                                    "\u2047\u2048\u2049\u204A\u204B\u204C\u204D\u204E\u204F\u2050\u2051\u2052\u2053\u2054\u2055\u2056\u2057\u2058\u2059" +
                                    "\u205A\u205B\u205C\u205D\u205E\u205F\u2060" +
                                    "\u2061\u2062\u2063\u2064\u206A\u206B\u206C\u206D\u206E\u206F" +
                                    "\u3000";

        private static readonly string koreanChars;
        private static string customChars; // JGMA added this

        static NameMaker()
        {
            string lUnicode = unicodeChars;
            for (int i = 0; i < lUnicode.Length; i++)
            {
                for (int j = i + 1; j < lUnicode.Length; j++)
                {
                    System.Diagnostics.Debug.Assert(lUnicode[i] != lUnicode[j], "Duplicate Char");
                }
            }

            UseUnicodeChars = false;

            // Fill the char array used for renaming with characters
            // from Hangul (Korean) unicode character set.
            var chars = new List<char>(128);
            var rnd = new Random();
            var startPoint = rnd.Next(0xAC00, 0xD5D0);
            for (int i = startPoint; i < startPoint + 128; i++)
                chars.Add((char) i);

            ShuffleArray(chars, rnd);
            koreanChars = new string(chars.ToArray());

            UseKoreanChars = false;

            // JGMA: Default for CustomChars is fales. This will get overridden separately, if applicable
            UseCustomChars = false; // JGMA added this
        }

        private static void ShuffleArray<T>(IList<T> list, Random rnd)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rnd.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static bool UseUnicodeChars
        {
            get { return uniqueChars1 == unicodeChars; }
            set
            {
                if (value)
                    uniqueChars1 = unicodeChars;
                else
                    uniqueChars1 = defaultChars;

                numUniqueChars = uniqueChars1.Length;
            }
        }

        public static bool UseKoreanChars
        {
            get { return uniqueChars1 == koreanChars; }
            set
            {
                if (value)
                    uniqueChars1 = koreanChars;
                else
                    uniqueChars1 = defaultChars;

                numUniqueChars = uniqueChars1.Length;
            }
        }

        // JGMA added this
        public static bool UseCustomChars
        {
            get { return uniqueChars1 == customChars; }
            set
            {
                if (value)
                    uniqueChars1 = customChars;
                else
                    uniqueChars1 = defaultChars;

                numUniqueChars = uniqueChars1.Length;
            }
        }

        // JGMA added this custom alphabet
        public static void DoUseCustomAlphabet(string AlphabetFile)
        {
            Console.Write("Custom alphabet: " + AlphabetFile + "... ");

            if (!System.IO.File.Exists(AlphabetFile))
            {
                Console.WriteLine("not found.");
                return;
            }

            string allCustom = System.IO.File.ReadAllText(AlphabetFile, Encoding.UTF8); // May need to customize encoding??
            int Max = allCustom.Length;
            if (Max < 128)
            {
                Console.WriteLine("too short (need at least 128 chars)");
                return;
            }

            var chars = new List<char>(128);
            var rnd = new Random();
            var startPoint = rnd.Next(0, Max-1);
            for (int i = 0; i < 128; i++)
            {
                // If we overrun, we loop round to the start
                if((startPoint + i) >= Max)
                    startPoint = -i;

                chars.Add((char)allCustom[i+startPoint]);
            }

            ShuffleArray(chars, rnd);
            customChars = new string(chars.ToArray());

            Console.WriteLine("loaded!");
            //Console.WriteLine("Alphabet: " + customChars);

            UseCustomChars = true;
        }

        public static string UniqueName(int index)
        {
            return UniqueName(index, null);
        }

        public static string UniqueName(int index, string sep)
        {
            // optimization for simple case
            if (index < numUniqueChars)
                return uniqueChars1[index].ToString();

            Stack<char> stack = new Stack<char>();

            do
            {
                stack.Push(uniqueChars1[index % numUniqueChars]);
                if (index < numUniqueChars)
                    break;
                index /= numUniqueChars;
            } while (true);

            StringBuilder builder = new StringBuilder();
            builder.Append(stack.Pop());
            while (stack.Count > 0)
            {
                if (sep != null)
                    builder.Append(sep);
                builder.Append(stack.Pop());
            }

            //Console.WriteLine("Unique name: '" + builder.ToString() + "'");

            return builder.ToString();
        }

        public static string UniqueNestedTypeName(int index)
        {
            return UniqueName(index, null);
        }

        public static string UniqueTypeName(int index)
        {
            return UniqueName(index % numUniqueChars, ".");
        }

        public static string UniqueNamespace(int index)
        {
            return UniqueName(index / numUniqueChars, ".");
        }
    }
}
