// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        // Matches StringsFile.Parse's key: an ASCII letter or underscore, followed by word characters;
        // subsequent dotted segments consist of one or more word characters.
        [GeneratedRegex(@"^[A-Za-z_]\w*(?:\.\w+)*$", RegexOptions.None, 1000)]
        private static partial Regex StringKey();

        bool ValidateStringKey(string? message, SourceLocation location)
        {
            if (message?.StartsWith("$strings.", StringComparison.Ordinal) is not true ||
                StringKey().IsMatch(message["$strings.".Length..]))
            {
                return true;
            }

            Error(DiagnosticCodes.InvalidSemanticStringKey, $"Message '{message}' is not a valid $strings.<key> reference.", location);
            return false;
        }
    }
}
