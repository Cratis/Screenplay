// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Contracts;

// Pattern matching is limited to runtime version names, binder issue messages and documentation specs.
// Contract facts are never extracted from embedded compiler or CLI source text.
static class ContractSources
{
    internal static IEnumerable<string> Matches(string source, string pattern) => Regex.Matches(source, pattern, RegexOptions.Singleline, TimeSpan.FromSeconds(2)).Select(match => match.Groups[1].Value);
}
