﻿/*
 * CSV Parser for C#.
 *
 * These codes are licensed under CC0.
 * https://github.com/yutokun/CSV-Parser
 */


using System;
using System.ComponentModel;

namespace yutokun
{
    public static class DelimiterExtensions
    {
        public static char ToChar(this Delimiter delimiter)
        {
            // C# 7.3: Unity 2018.2 - 2020.1 Compatible
            switch (delimiter)
            {
                case Delimiter.Auto:
                    throw new InvalidEnumArgumentException("Could not return char of Delimiter.Auto.");
                case Delimiter.Comma:
                    return ',';
                case Delimiter.Tab:
                    return '\t';
                case Delimiter.Semicolon:
                    return ';';
                default:
                    throw new ArgumentOutOfRangeException(nameof(delimiter), delimiter, null);
            }
        }
    }
}
