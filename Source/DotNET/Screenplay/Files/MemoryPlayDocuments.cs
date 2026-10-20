// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Admits in-memory folder documents and locates them relative to the compilation root.
/// </summary>
internal sealed class MemoryPlayDocuments
{
    readonly Dictionary<string, string> _paths = new(StringComparer.Ordinal);

    public MemoryPlayDocuments(IEnumerable<PlayFileContent> documents, string? entry = null)
    {
        ArgumentNullException.ThrowIfNull(documents);
        var contents = ReadDocuments(documents);
        string[] folder = [];
        if (entry is not null)
        {
            var path = NormalizePath(entry);
            if (!contents.ContainsKey(path))
            {
                throw new ArgumentException($"Entry document '{entry}' is not in the document set.", nameof(entry));
            }

            folder = path.Split('/')[..^1];
        }

        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (path, content) in contents.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var relative = RelativeTo(folder, path);
            _paths.Add(relative, path);
            sources.Add(relative, content);
        }

        Source = new MemoryDocumentSource(new(sources), _paths, string.Join('/', folder));
    }

    public IPlayDocumentSource Source { get; }

    public PlayFile File(string path) => new(_paths[path], path);

    internal static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path[0] is '/' or '\\' || path.IndexOfAny([':', '*', '?', '\0']) >= 0)
        {
            throw new ArgumentException("Document paths must be relative .play paths within the folder.", nameof(path));
        }

        var normalized = PlayGlob.Normalize(path);
        if (normalized.StartsWith("../", StringComparison.Ordinal) || !normalized.EndsWith(".play", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Document path '{path}' must name a .play file within the folder.", nameof(path));
        }

        return normalized;
    }

    static Dictionary<string, string> ReadDocuments(IEnumerable<PlayFileContent> documents)
    {
        var contents = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var document in documents)
        {
            if (document is null || document.Content is null)
            {
                throw new ArgumentException("Documents and their source texts must not be null.", nameof(documents));
            }

            var path = NormalizePath(document.RelativePath);
            if (!contents.TryAdd(path, document.Content))
            {
                throw new ArgumentException($"Duplicate document path '{path}'.", nameof(documents));
            }
        }

        return contents;
    }

    static string RelativeTo(string[] folder, string path)
    {
        var segments = path.Split('/');
        var shared = 0;
        while (shared < folder.Length && shared < segments.Length && folder[shared] == segments[shared])
        {
            shared++;
        }

        return string.Join('/', Enumerable.Repeat("..", folder.Length - shared).Concat(segments.Skip(shared)));
    }

    sealed class MemoryDocumentSource(InMemoryPlayDocumentSource source, IReadOnlyDictionary<string, string> paths, string entryFolder) : IPlayDocumentSource
    {
        /// <inheritdoc/>
        public IEnumerable<string> FilesBeneath(string folder)
        {
            var resolved = PlayGlob.Normalize($"{entryFolder}/{folder}");
            return paths.Where(pair => IsBeneath(resolved, pair.Value)).Select(pair => pair.Key);
        }

        /// <inheritdoc/>
        public string Read(string path) => source.Read(path);

        static bool IsBeneath(string folder, string path) =>
            // An ancestor of the document-set root contains every supplied document.
            folder.Length == 0 || folder.Split('/').All(segment => segment == "..") || path.StartsWith($"{folder}/", StringComparison.Ordinal);
    }
}
