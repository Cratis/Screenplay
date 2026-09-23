// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed class McpRoot
{
    internal const int MaximumFiles = 512;
    internal const int MaximumBytes = 8 * 1024 * 1024;
    internal const int MaximumFileBytes = 2 * 1024 * 1024;
    const int MaximumEntries = 32768;
    static readonly HashSet<string> _excludedDirectories = new(StringComparer.OrdinalIgnoreCase) { ".git", ".ai-work", ".screenplay", "bin", "obj", "node_modules" };
    readonly string _path;

    internal McpRoot(string path)
    {
        _path = Path.GetFullPath(path);
        CheckAncestors(_path);
        if (!Directory.Exists(_path))
        {
            throw new McpFailure("The MCP root must be an existing directory.");
        }
    }

    internal string ApplicationName => new DirectoryInfo(_path).Name;

    internal string DirectoryPath
    {
        get
        {
            CheckAncestors(_path);
            return _path;
        }
    }

    internal static void CheckDocuments(ImmutableArray<WorkspaceDocument> documents, bool allowEmpty = false)
    {
        if (documents.IsDefault || (!allowEmpty && documents.IsEmpty) || documents.Length > MaximumFiles || documents.Sum(document => document.Bytes.Length) > MaximumBytes)
        {
            throw new McpFailure($"Workspace must contain 1–{MaximumFiles} files and at most {MaximumBytes} source bytes.");
        }

        foreach (var document in documents)
        {
            CheckSource(document);
        }
    }

    internal static void CheckAncestors(string path)
    {
        for (var current = path; current is not null; current = Path.GetDirectoryName(current))
        {
            if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new McpFailure($"Symbolic links and reparse points are not admitted: '{current}'.");
            }
        }
    }

    internal ImmutableArray<WorkspaceDocument> Read(bool allowEmpty = false)
    {
        CheckAncestors(_path);
        var directories = new Stack<(string Path, int Depth)>();
        directories.Push((_path, 0));
        var documents = ImmutableArray.CreateBuilder<WorkspaceDocument>();
        var entries = 0;
        var bytes = 0;
        while (directories.TryPop(out var current))
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(current.Path))
            {
                if (++entries > MaximumEntries)
                {
                    throw new McpFailure($"Root exceeds {MaximumEntries} directory entries; choose a narrower root.");
                }

                var attributes = File.GetAttributes(entry);
                if (Path.GetFileName(entry).Equals(".screenplay", StringComparison.OrdinalIgnoreCase))
                {
                    if (attributes.HasFlag(FileAttributes.ReparsePoint) || !attributes.HasFlag(FileAttributes.Directory))
                    {
                        throw new McpFailure("ReservedPath: .screenplay must be a real, root-local metadata directory.");
                    }

                    continue;
                }

                if (attributes.HasFlag(FileAttributes.Directory) && _excludedDirectories.Contains(Path.GetFileName(entry)))
                {
                    continue;
                }

                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new McpFailure($"Symbolic links and reparse points are not admitted: '{entry}'.");
                }

                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    if (current.Depth >= 16)
                    {
                        throw new McpFailure("Root exceeds 16 directory levels; choose a narrower root.");
                    }

                    directories.Push((entry, current.Depth + 1));
                    continue;
                }

                if (!entry.EndsWith(".play", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (documents.Count >= MaximumFiles)
                {
                    throw new McpFailure($"Root exceeds {MaximumFiles} .play files.");
                }

                var relative = Path.GetRelativePath(_path, entry).Replace('\\', '/');
                var portable = PortablePlayPath.Parse(relative);
                var content = ReadBounded(entry);
                bytes += content.Length;
                if (bytes > MaximumBytes)
                {
                    throw new McpFailure($"Root exceeds {MaximumBytes} bytes of .play source.");
                }

                var document = WorkspaceDocument.Create(McpDocumentKeys.For(relative), portable, content);
                CheckSource(document);
                documents.Add(document);
            }
        }

        if (documents.Count == 0 && !allowEmpty)
        {
            throw new McpFailure("The root contains no .play files.");
        }

        return [.. documents.OrderBy(document => document.Path.Value, StringComparer.Ordinal)];
    }

    internal string PathFor(PortablePlayPath path, bool createParents = false)
    {
        CheckAncestors(_path);
        var segments = path.Value.Split('/');
        if (segments.Length > 17 || segments.Any(_excludedDirectories.Contains))
        {
            throw new McpFailure($"Path '{path}' is outside the admitted source scope.");
        }

        var full = Path.GetFullPath(Path.Combine(_path, path.Value));
        if (!full.StartsWith(_path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new McpFailure($"Path '{path}' escapes the root.");
        }

        var current = _path;
        foreach (var segment in segments[..^1])
        {
            current = Path.Combine(current, segment);
            McpManagedFiles.CheckExisting(current);
            if (Directory.Exists(current) || File.Exists(current))
            {
                CheckAncestors(current);
                if (!Directory.Exists(current))
                {
                    throw new McpFailure($"A file blocks directory '{current}'.");
                }
            }
            else if (createParents)
            {
                Directory.CreateDirectory(current);
                CheckAncestors(current);
            }
        }

        McpManagedFiles.CheckExisting(full);
        if (File.Exists(full) || Directory.Exists(full))
        {
            CheckAncestors(full);
        }

        return full;
    }

    internal void Verify(ScreenplayWorkspace workspace)
    {
        var current = Read(allowEmpty: true).ToDictionary(document => document.Path.Value, StringComparer.Ordinal);
        if (current.Count != workspace.Documents.Length || workspace.Documents.Any(document =>
            !current.TryGetValue(document.Path.Value, out var actual) || !actual.Bytes.AsSpan().SequenceEqual(document.Bytes.AsSpan())))
        {
            throw new McpFailure("DiskDrift: the .play file set or exact bytes differ from the workspace. Reopen before proposing changes.");
        }
    }

    static void CheckSource(WorkspaceDocument document)
    {
        if (document.Bytes.Length > MaximumFileBytes)
        {
            throw new McpFailure($"'{document.Path}' exceeds {MaximumFileBytes} bytes.");
        }

        var lines = document.Text.Split('\n');
        if (lines.Length > 20000 || lines.Any(line => line.Length > 8192 || line.TakeWhile(char.IsWhiteSpace).Count() > 128))
        {
            throw new McpFailure($"'{document.Path}' exceeds line length, line count, or indentation limits.");
        }
    }

    static byte[] ReadBounded(string path)
    {
        CheckAncestors(path);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaximumFileBytes)
        {
            throw new McpFailure($"'{path}' exceeds {MaximumFileBytes} bytes.");
        }

        var content = new byte[checked((int)stream.Length)];
        stream.ReadExactly(content);
        if (stream.ReadByte() != -1)
        {
            throw new McpFailure($"'{path}' changed while being read.");
        }

        CheckAncestors(path);
        return content;
    }
}
