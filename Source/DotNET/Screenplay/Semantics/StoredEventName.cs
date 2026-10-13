// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Semantics;

internal static partial class StoredEventName
{
    internal static bool IsValid(string value) => Identifier().IsMatch(value);

    [GeneratedRegex(@"\A[A-Za-z_]\w*\z", RegexOptions.None, 1000)]
    private static partial Regex Identifier();
}
