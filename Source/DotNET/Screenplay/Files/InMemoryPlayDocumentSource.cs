// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents an <see cref="IPlayDocumentSource"/> over documents held in memory, keyed by portable path.
/// </summary>
/// <param name="documents">The source text of each document by its portable path.</param>
public class InMemoryPlayDocumentSource(IReadOnlyDictionary<string, string> documents) : IPlayDocumentSource
{
    /// <inheritdoc/>
    public IEnumerable<string> FilesBeneath(string folder) =>
        documents.Keys.Where(path => folder.Length == 0 || path.StartsWith($"{folder}/", StringComparison.Ordinal));

    /// <inheritdoc/>
    public string Read(string path) => documents[path];
}
