// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Contracts;

static class ContractSources
{
    internal static readonly IReadOnlyDictionary<string, string> All = Read();

    internal static IEnumerable<string> Matches(string source, string pattern) => Regex.Matches(source, pattern, RegexOptions.Singleline, TimeSpan.FromSeconds(2)).Select(match => match.Groups[1].Value);

    internal static string Get(string path) => All.TryGetValue(path, out var source) ? source : throw new InvalidScreenplayContract($"Missing embedded contract source '{path}'.");

    static Dictionary<string, string> Read()
    {
        var assembly = typeof(ContractSources).Assembly;
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in assembly.GetManifestResourceNames().Where(name => name.StartsWith("compiler/", StringComparison.Ordinal) || name.StartsWith("cli/", StringComparison.Ordinal)))
        {
            using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
            result.Add(name.Replace('\\', '/'), reader.ReadToEnd());
        }

        return result;
    }
}
