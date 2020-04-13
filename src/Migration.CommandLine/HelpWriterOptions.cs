﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Roslynator
{
    public class HelpWriterOptions
    {
        public static HelpWriterOptions Default { get; } = new HelpWriterOptions();

        public HelpWriterOptions(
            bool includeValues = false,
            string indent = "  ")
        {
            IncludeValues = includeValues;
            Indent = indent;
        }

        public bool IncludeValues { get; }

        public string Indent { get; }
    }
}
