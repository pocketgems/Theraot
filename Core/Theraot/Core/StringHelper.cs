﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Theraot.Collections;

namespace Theraot.Core
{
    [global::System.Diagnostics.DebuggerNonUserCode]
    public static class StringHelper
    {
        public static string Append(this string text, string value)
        {
            return string.Concat(text, value);
        }

        public static string Append(this string text, string value1, string value2)
        {
            return string.Concat(text, value1, value2);
        }

        public static string Append(this string text, string value1, string value2, string value3)
        {
            return string.Concat(text, value1, value2, value3);
        }

        public static string Append(this string text, params string[] values)
        {
            return string.Concat(text, values);
        }

        public static string Concat(params string[] value)
        {
            return string.Concat(value);
        }

        public static string Concat(string[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", "Non-negative number is required.");
            }
            if (arrayIndex == array.Length)
            {
                return string.Empty;
            }
            return ConcatExtracted(array, arrayIndex, array.Length - arrayIndex);
        }

        public static string Concat(string[] array, int arrayIndex, int countLimit)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", "Non-negative number is required.");
            }
            if (countLimit < 0)
            {
                throw new ArgumentOutOfRangeException("countLimit", "Non-negative number is required.");
            }
            if (countLimit > array.Length - arrayIndex)
            {
                throw new ArgumentException("array", "startIndex plus countLimit is greater than the number of elements in array.");
            }
            if (arrayIndex == array.Length)
            {
                return string.Empty;
            }
            return ConcatExtracted(array, arrayIndex, countLimit);
        }

        public static string Concat(params object[] values)
        {
            return string.Concat(values);
        }

        public static string Concat(object[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", "Non-negative number is required.");
            }
            if (arrayIndex == array.Length)
            {
                return string.Empty;
            }
            return ConcatExtracted(array, arrayIndex, array.Length - arrayIndex);
        }

        public static string Concat(object[] array, int arrayIndex, int countLimit)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", "Non-negative number is required.");
            }
            if (countLimit < 0)
            {
                throw new ArgumentOutOfRangeException("countLimit", "Non-negative number is required.");
            }
            if (countLimit > array.Length - arrayIndex)
            {
                throw new ArgumentException("array", "startIndex plus countLimit is greater than the number of elements in array.");
            }
            if (arrayIndex == array.Length)
            {
                return string.Empty;
            }
            return ConcatExtracted(array, arrayIndex, countLimit);
        }

        public static string Concat(IEnumerable<string> values)
        {
#if NET20 || NET30 || NET35
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            var stringList = new List<string>();
            int length = 0;
            foreach (var item in values)
            {
                stringList.Add(item);
                length += item.Length;
            }
            return ConcatExtractedExtracted(stringList.ToArray(), 0, stringList.Count, length);
#else
            return string.Concat(values);
#endif
        }

        public static string Concat<T>(IEnumerable<T> values)
        {
#if NET20 || NET30 || NET35
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            var stringList = new List<string>();
            int length = 0;
            foreach (var item in values)
            {
                var itemToString = item.ToString();
                stringList.Add(itemToString);
                length += itemToString.Length;
            }
            return ConcatExtractedExtracted(stringList.ToArray(), 0, stringList.Count, length);
#else
            return string.Concat(values);
#endif
        }

        public static string End(this string text, int characterCount)
        {
            var _text = Check.NotNullArgument(text, "text");
            int length = _text.Length;
            if (length < characterCount)
            {
                return _text;
            }
            else
            {
                return _text.Substring(length - characterCount);
            }
        }

        public static string EnsureEnd(this string text, string end)
        {
            var _text = Check.NotNullArgument(text, "text");
            if (!_text.EndsWith(end, false, CultureInfo.CurrentCulture))
            {
                return _text.Append(end);
            }
            else
            {
                return _text;
            }
        }

        public static string EnsureEnd(this string text, string end, bool ignoreCase, CultureInfo culture)
        {
            var _text = Check.NotNullArgument(text, "text");
            if (!_text.EndsWith(end, ignoreCase, culture))
            {
                return _text.Append(end);
            }
            else
            {
                return _text;
            }
        }

        public static string EnsureEnd(this string text, string end, StringComparison comparisonType)
        {
            var _text = Check.NotNullArgument(text, "text");
            if (!_text.EndsWith(end, comparisonType))
            {
                return _text.Append(end);
            }
            else
            {
                return _text;
            }
        }

        public static string EnsureStart(this string text, string start)
        {
            var _text = Check.NotNullArgument(text, "text");
            if (!_text.StartsWith(start, false, CultureInfo.CurrentCulture))
            {
                return start.Append(_text);
            }
            else
            {
                return _text;
            }
        }

        public static string EnsureStart(this string text, string start, bool ignoreCase, CultureInfo culture)
        {
            var _text = Check.NotNullArgument(text, "text");
            if (!_text.StartsWith(start, ignoreCase, culture))
            {
                return start.Append(_text);
            }
            else
            {
                return _text;
            }
        }

        public static string EnsureStart(this string text, string start, StringComparison comparisonType)
        {
            var _text = Check.NotNullArgument(text, "text");
            if (!_text.StartsWith(start, comparisonType))
            {
                return start.Append(_text);
            }
            else
            {
                return _text;
            }
        }

        public static string ExceptEnd(this string text, int characterCount)
        {
            var _text = Check.NotNullArgument(text, "text");
            int length = _text.Length;
            if (length < characterCount)
            {
                return string.Empty;
            }
            else
            {
                return _text.Substring(0, length - characterCount);
            }
        }

        public static string ExceptStart(this string text, int characterCount)
        {
            var _text = Check.NotNullArgument(text, "text");
            int length = _text.Length;
            if (length < characterCount)
            {
                return string.Empty;
            }
            else
            {
                return _text.Substring(characterCount);
            }
        }

        public static string Implode<TInput>(IEnumerable<TInput> collection, Converter<TInput, string> converter, string separator, string open, string close)
        {
            return Implode(new ConversionSet<TInput, string>(collection, converter), separator, open, close);
        }

        public static string Implode<TInput>(IEnumerable<TInput> collection, Converter<TInput, string> converter, string separator)
        {
            return Implode(new ConversionSet<TInput, string>(collection, converter), separator);
        }

        public static string Implode(IEnumerable<string> collection, string separator)
        {
            StringBuilder str = ImplodeExtracted(collection, separator);
            return str.ToString();
        }

        public static string Implode(IEnumerable<string> collection, string separator, string open, string close)
        {
            StringBuilder str = ImplodeExtracted(collection, separator);
            if (str.Length > 0)
            {
                return open.Safe() + str.ToString() + close.Safe();
            }
            else
            {
                return str.ToString();
            }
        }

        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static bool IsNullOrWhiteSpace(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }
            foreach (char character in value)
            {
                if (!char.IsWhiteSpace(character))
                {
                    return false;
                }
            }
            return true;
        }

        public static string Join(string separator, params string[] value)
        {
            if (ReferenceEquals(value, null))
            {
                throw new ArgumentNullException("value");
            }
            if (ReferenceEquals(separator, null))
            {
                separator = string.Empty;
            }
            return JoinExtracted(separator, value, 0, value.Length);
        }

        public static string Join(string separator, string[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", "Non-negative number is required.");
            }
            if (arrayIndex == array.Length)
            {
                return string.Empty;
            }
            if (ReferenceEquals(separator, null))
            {
                separator = string.Empty;
            }
            return JoinExtracted(separator, array, arrayIndex, array.Length - arrayIndex);
        }

        public static string Join(string separator, string[] array, int arrayIndex, int countLimit)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", "Non-negative number is required.");
            }
            if (countLimit < 0)
            {
                throw new ArgumentOutOfRangeException("countLimit", "Non-negative number is required.");
            }
            if (countLimit > array.Length - arrayIndex)
            {
                throw new ArgumentException("array", "The array can not contain the number of elements.");
            }
            if (arrayIndex == array.Length)
            {
                return string.Empty;
            }
            if (ReferenceEquals(separator, null))
            {
                separator = string.Empty;
            }
            return JoinExtracted(separator, array, arrayIndex, countLimit);
        }

        public static string Join(string separator, params object[] values)
        {
            if (separator == null)
            {
                return string.Concat(values);
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            var array = new string[values.Length];
            int index = 0;
            foreach (var item in values)
            {
                array[index++] = item.ToString();
            }
            return JoinExtracted(separator, array, 0, array.Length);
        }

        public static string Join<T>(string separator, IEnumerable<T> values)
        {
            if (separator == null)
            {
                return Concat<T>(values);
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            var stringList = new List<string>();
            foreach (var item in values)
            {
                stringList.Add(item.ToString());
            }
            return JoinExtracted(separator, stringList.ToArray(), 0, stringList.Count);
        }

        public static bool Like(this string text, Regex regex, int startAt)
        {
            return regex.IsMatch(text, startAt);
        }

        public static bool Like(this string text, Regex regex)
        {
            return text.Like(regex, 0);
        }

        public static bool Like(this string text, string regexPattern, RegexOptions regexOptions, int startAt)
        {
            var regex = new Regex(regexPattern, regexOptions);
            return regex.IsMatch(text, startAt);
        }

        public static bool Like(this string text, string regexPattern, RegexOptions regexOptions)
        {
            return text.Like(regexPattern, regexOptions, 0);
        }

        public static bool Like(this string text, string regexPattern, bool ignoreCase)
        {
            return text.Like(regexPattern, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None, 0);
        }

        public static bool Like(this string text, string regexPattern)
        {
            return text.Like(regexPattern, RegexOptions.IgnoreCase, 0);
        }

        public static bool Like(this string text, string regexPattern, bool ignoreCase, int startAt)
        {
            return text.Like(regexPattern, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None, startAt);
        }

        public static bool Like(this string text, string regexPattern, int startAt)
        {
            return text.Like(regexPattern, RegexOptions.IgnoreCase, startAt);
        }

        public static Match Match(this string text, Regex regex, int startAt, int length)
        {
            return regex.Match(text, startAt, length);
        }

        public static Match Match(this string text, Regex regex, int startAt)
        {
            return regex.Match(text, startAt);
        }

        public static Match Match(this string text, Regex regex)
        {
            return text.Match(regex, 0);
        }

        public static Match Match(this string text, string regexPattern, RegexOptions regexOptions, int startAt, int length)
        {
            var regex = new Regex(regexPattern, regexOptions);
            return regex.Match(text, startAt, length);
        }

        public static Match Match(this string text, string regexPattern, RegexOptions regexOptions, int startAt)
        {
            var regex = new Regex(regexPattern, regexOptions);
            return regex.Match(text, startAt);
        }

        public static Match Match(this string text, string regexPattern, RegexOptions regexOptions)
        {
            return text.Match(regexPattern, regexOptions, 0);
        }

        public static Match Match(this string text, string regexPattern, bool ignoreCase, int startAt, int length)
        {
            return text.Match(regexPattern, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None, startAt, length);
        }

        public static Match Match(this string text, string regexPattern, bool ignoreCase, int startAt)
        {
            return text.Match(regexPattern, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None, startAt);
        }

        public static Match Match(this string text, string regexPattern, bool ignoreCase)
        {
            return text.Match(regexPattern, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None, 0);
        }

        public static Match Match(this string text, string regexPattern, int startAt, int length)
        {
            return text.Match(regexPattern, RegexOptions.IgnoreCase, startAt, length);
        }

        public static Match Match(this string text, string regexPattern, int startAt)
        {
            return text.Match(regexPattern, RegexOptions.IgnoreCase, startAt);
        }

        public static Match Match(this string text, string regexPattern)
        {
            return text.Match(regexPattern, RegexOptions.IgnoreCase, 0);
        }

        public static MatchCollection Matches(this string text, Regex regex, int startAt)
        {
            return regex.Matches(text, startAt);
        }

        public static MatchCollection Matches(this string text, Regex regex)
        {
            return text.Matches(regex, 0);
        }

        public static MatchCollection Matches(this string text, string regexPattern, RegexOptions regexOptions, int startAt)
        {
            var regex = new Regex(regexPattern, regexOptions);
            return regex.Matches(text, startAt);
        }

        public static MatchCollection Matches(this string text, string regexPattern, RegexOptions regexOptions)
        {
            return text.Matches(regexPattern, regexOptions, 0);
        }

        public static MatchCollection Matches(this string text, string regexPattern, bool ignoreCase, int startAt)
        {
            var regex = new Regex(regexPattern, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            return regex.Matches(text, startAt);
        }

        public static MatchCollection Matches(this string text, string regexPattern, bool ignoreCase)
        {
            return text.Matches(regexPattern, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None, 0);
        }

        public static MatchCollection Matches(this string text, string regexPattern, int startAt)
        {
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
            return regex.Matches(text, startAt);
        }

        public static MatchCollection Matches(this string text, string regexPattern)
        {
            return text.Matches(regexPattern, RegexOptions.IgnoreCase, 0);
        }

        public static string NeglectEnd(this string text, string end)
        {
            var _text = Check.NotNullArgument(text, "text");
            var _end = Check.NotNullArgument(end, "end");
            if (_text.EndsWith(_end, false, CultureInfo.CurrentCulture))
            {
                return _text.ExceptEnd(_end.Length);
            }
            else
            {
                return _text;
            }
        }

        public static string NeglectEnd(this string text, string end, bool ignoreCase, CultureInfo culture)
        {
            var _text = Check.NotNullArgument(text, "text");
            var _end = Check.NotNullArgument(end, "end");
            if (_text.EndsWith(_end, ignoreCase, culture))
            {
                return _text.ExceptEnd(_end.Length);
            }
            else
            {
                return _text;
            }
        }

        public static string NeglectEnd(this string text, string end, StringComparison comparisonType)
        {
            var _text = Check.NotNullArgument(text, "text");
            var _end = Check.NotNullArgument(end, "end");
            if (_text.EndsWith(_end, comparisonType))
            {
                return _text.ExceptEnd(_end.Length);
            }
            else
            {
                return _text;
            }
        }

        public static string NeglectStart(this string text, string start)
        {
            var _text = Check.NotNullArgument(text, "text");
            var _start = Check.NotNullArgument(start, "start");
            if (_text.StartsWith(_start, false, CultureInfo.CurrentCulture))
            {
                return _text.ExceptStart(_start.Length);
            }
            else
            {
                return _text;
            }
        }

        public static string NeglectStart(this string text, string start, bool ignoreCase, CultureInfo culture)
        {
            var _text = Check.NotNullArgument(text, "text");
            var _start = Check.NotNullArgument(start, "start");
            if (_text.StartsWith(_start, ignoreCase, culture))
            {
                return _text.ExceptStart(_start.Length);
            }
            else
            {
                return _text;
            }
        }

        public static string NeglectStart(this string text, string start, StringComparison comparisonType)
        {
            var _text = Check.NotNullArgument(text, "text");
            var _start = Check.NotNullArgument(start, "start");
            if (_text.StartsWith(_start, comparisonType))
            {
                return _text.ExceptStart(_start.Length);
            }
            else
            {
                return _text;
            }
        }

        public static string Safe(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            else
            {
                return text;
            }
        }

        public static string Start(this string text, int characterCount)
        {
            var _text = Check.NotNullArgument(text, "text");
            int length = _text.Length;
            if (length < characterCount)
            {
                return _text;
            }
            else
            {
                return _text.Substring(0, characterCount);
            }
        }

        private static string ConcatExtracted(object[] array, int startIndex, int count)
        {
            int length = 0;
            int maxIndex = startIndex + count;
            var newArray = new string[count];
            for (int index = startIndex; index < maxIndex; index++)
            {
                var item = array[index];
                if (!ReferenceEquals(item, null))
                {
                    var itemToString = item.ToString();
                    newArray[index - startIndex] = itemToString;
                    length += itemToString.Length;
                }
            }
            return ConcatExtractedExtracted(newArray, 0, count, length);
        }

        private static string ConcatExtracted(string[] array, int startIndex, int count)
        {
            int length = 0;
            int maxIndex = startIndex + count;
            for (int index = startIndex; index < maxIndex; index++)
            {
                var item = array[index];
                if (!ReferenceEquals(item, null))
                {
                    length += item.Length;
                }
            }
            return ConcatExtractedExtracted(array, startIndex, maxIndex, length);
        }

        private static string ConcatExtractedExtracted(string[] array, int startIndex, int maxIndex, int length)
        {
            if (length <= 0)
            {
                return string.Empty;
            }
            else
            {
                StringBuilder result = new StringBuilder(length);
                for (int index = startIndex; index < maxIndex; index++)
                {
                    var item = array[index];
                    result.Append(item);
                }
                return result.ToString();
            }
        }

        private static StringBuilder ImplodeExtracted(IEnumerable<string> collection, string separator)
        {
            var str = new StringBuilder();
            bool first = true;
            foreach (string item in Check.NotNullArgument(collection, "collection"))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    str.Append(separator);
                }
                str.Append(item);
            }
            return str;
        }

        private static string JoinExtracted(string separator, string[] array, int startIndex, int count)
        {
            int length = 0;
            int maxIndex = startIndex + count;
            for (int index = startIndex; index < maxIndex; index++)
            {
                var item = array[index];
                if (!ReferenceEquals(item, null))
                {
                    length += item.Length;
                }
            }
            length += separator.Length * (count - 1);
            if (length <= 0)
            {
                return string.Empty;
            }
            else
            {
                StringBuilder result = new StringBuilder(length);
                bool first = true;
                for (int index = startIndex; index < maxIndex; index++)
                {
                    var item = array[index];
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        result.Append(separator);
                    }
                    result.Append(item);
                }
                return result.ToString();
            }
        }

        /*[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Justification = "By Design")]
        public static string ToString(this object obj, string onNull)
        {
            if (obj == null)
            {
                return onNull;
            }
            else
            {
                return obj.ToString();
            }
        }*/
    }
}