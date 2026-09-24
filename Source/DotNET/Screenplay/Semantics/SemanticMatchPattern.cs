// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Holds the portable match rule definition shared by binding, contract validation and execution.
/// </summary>
internal static class SemanticMatchPattern
{
    // Exactly one @; nonempty local part without whitespace or @; at least two nonempty domain labels.
    internal const string Email = @"^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$";

    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    internal static Regex Create(string pattern) => new(pattern, RegexOptions.ECMAScript | RegexOptions.CultureInvariant, Timeout);
}
