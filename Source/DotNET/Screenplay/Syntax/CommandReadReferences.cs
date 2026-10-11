// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

internal static class CommandReadReferences
{
    internal static IEnumerable<string> VisibleNames(IEnumerable<ReadsSyntax> source)
    {
        var declarations = source.ToArray();

        return declarations.Where(read => declarations.Count(other => other.ReadModel == read.ReadModel) == 1)
            .Select(read => read.ReadModel)
            .Concat(declarations.Select(read => read.Alias).OfType<string>());
    }

    internal static ReadsSyntax[] OperationSources(IEnumerable<ReadsSyntax> reads, string name) =>
        [.. reads.Where(read => (read.Alias ?? read.ReadModel) == name)];

    internal static IEnumerable<(ReadsSyntax Read, string Path)> Paths(IEnumerable<ReadsSyntax> source, string path)
    {
        var reads = source.ToArray();
        var names = VisibleNames(reads).Where(name => path.StartsWith($"{name}.", StringComparison.Ordinal)).ToArray();
        var longest = names.Length == 0 ? 0 : names.Max(name => name.Length);

        return names.Where(name => name.Length == longest).SelectMany(name => reads
            .Where(read => read.Alias == name || read.ReadModel == name)
            .Select(read => (read, path[(name.Length + 1)..])));
    }
}
