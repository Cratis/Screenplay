// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// The bounded, read-only file rules of a model root shared by the MCP server and <see cref="ScreenplayModelRoot"/>.
/// </summary>
/// <remarks>
/// Only <c>Create</c> and <c>PathFor</c> with <c>createParents</c> write; opening a root never calls them.
/// </remarks>
sealed class ModelRootDirectory
{
    internal const int MaximumFiles = 512;
    internal const int MaximumBytes = 8 * 1024 * 1024;
    internal const int MaximumFileBytes = 2 * 1024 * 1024;
    const int MaximumEntries = 32768;
    static readonly HashSet<string> _excludedDirectories = new(StringComparer.OrdinalIgnoreCase) { ".git", ".ai-work", ".screenplay", "bin", "obj", "node_modules" };
    readonly string _path;
    readonly int _missingLevels;

    // missingLevels bounds how many trailing folders may not exist yet. A path a host or user names may only miss its
    // last folder, so a mistyped path never creates a tree; the server's own Documents/Screenplay fallback may miss two.
    internal ModelRootDirectory(string path, int missingLevels = 1)
    {
        _path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        _missingLevels = missingLevels;
        CheckDirectory();
    }

    internal string ApplicationName => new DirectoryInfo(_path).Name;

    // Rebinding must not dereference a previous root that has been removed or made unreadable.
    internal bool Exists => Directory.Exists(_path);

    internal string DirectoryPath
    {
        get
        {
            CheckDirectory();
            return _path;
        }
    }

    // Bootstrap once; subsequent moves retain this opaque key through workspace serialization.
    internal static string DocumentKey(string path) => $"mcp:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path)))}";

    internal static void CheckDocuments(ImmutableArray<WorkspaceDocument> documents, bool allowEmpty = false)
    {
        if (documents.IsDefault || (!allowEmpty && documents.IsEmpty) || documents.Length > MaximumFiles || documents.Sum(document => document.Bytes.Length) > MaximumBytes)
        {
            throw new ModelRootRefused($"Workspace must contain 1–{MaximumFiles} files and at most {MaximumBytes} source bytes.");
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
                throw new ModelRootRefused($"Symbolic links and reparse points are not admitted: '{current}'.");
            }
        }
    }

    internal bool SamePath(ModelRootDirectory other) => string.Equals(_path, other._path, StringComparison.Ordinal);

    internal void Create()
    {
        CheckDirectory();
        Directory.CreateDirectory(_path);
        CheckAncestors(_path);
    }

    internal ImmutableArray<WorkspaceDocument> Read(bool allowEmpty = false)
    {
        CheckDirectory();
        var directories = TraversalStart();
        var documents = ImmutableArray.CreateBuilder<WorkspaceDocument>();
        var entries = 0;
        var bytes = 0;
        while (directories.TryPop(out var current))
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(current.Path))
            {
                if (++entries > MaximumEntries)
                {
                    throw new ModelRootRefused($"Root exceeds {MaximumEntries} directory entries; choose a narrower root.");
                }

                var attributes = File.GetAttributes(entry);
                if (Path.GetFileName(entry).Equals(".screenplay", StringComparison.OrdinalIgnoreCase))
                {
                    if (attributes.HasFlag(FileAttributes.ReparsePoint) || !attributes.HasFlag(FileAttributes.Directory))
                    {
                        throw new ModelRootRefused("ReservedPath: .screenplay must be a real, root-local metadata directory.");
                    }

                    continue;
                }

                if (attributes.HasFlag(FileAttributes.Directory) && _excludedDirectories.Contains(Path.GetFileName(entry)))
                {
                    continue;
                }

                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new ModelRootRefused($"Symbolic links and reparse points are not admitted: '{entry}'.");
                }

                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    if (current.Depth >= 16)
                    {
                        throw new ModelRootRefused("Root exceeds 16 directory levels; choose a narrower root.");
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
                    throw new ModelRootRefused($"Root exceeds {MaximumFiles} .play files.");
                }

                var relative = Path.GetRelativePath(_path, entry).Replace('\\', '/');
                var portable = PortablePlayPath.Parse(relative);
                var content = ReadBounded(entry);
                bytes += content.Length;
                if (bytes > MaximumBytes)
                {
                    throw new ModelRootRefused($"Root exceeds {MaximumBytes} bytes of .play source.");
                }

                var document = WorkspaceDocument.Create(DocumentKey(relative), portable, content);
                CheckSource(document);
                documents.Add(document);
            }
        }

        if (documents.Count == 0 && !allowEmpty)
        {
            throw new ModelRootRefused("The root contains no .play files.");
        }

        return [.. documents.OrderBy(document => document.Path.Value, StringComparer.Ordinal)];
    }

    internal string PathFor(PortablePlayPath path, bool createParents = false)
    {
        CheckDirectory();
        var segments = path.Value.Split('/');
        if (segments.Length > 17 || segments.Any(_excludedDirectories.Contains))
        {
            throw new ModelRootRefused($"Path '{path}' is outside the admitted source scope.");
        }

        var full = Path.GetFullPath(Path.Combine(_path, path.Value));
        if (!full.StartsWith(_path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ModelRootRefused($"Path '{path}' escapes the root.");
        }

        CheckParents(segments[..^1], createParents);
        ModelRootMetadata.CheckExisting(full);
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
            throw new ModelRootRefused("DiskDrift: the .play file set or exact bytes differ from the workspace. Reopen before proposing changes.", "DiskDrift") { McpFailureKind = "DiskDrift" };
        }
    }

    static void CheckSource(WorkspaceDocument document)
    {
        if (document.Bytes.Length > MaximumFileBytes)
        {
            throw new ModelRootRefused($"'{document.Path}' exceeds {MaximumFileBytes} bytes.");
        }

        var lines = document.Text.Split('\n');
        if (lines.Length > 20000 || lines.Any(line => line.Length > 8192 || line.TakeWhile(char.IsWhiteSpace).Count() > 128))
        {
            throw new ModelRootRefused($"'{document.Path}' exceeds line length, line count, or indentation limits.");
        }
    }

    static byte[] ReadBounded(string path)
    {
        CheckAncestors(path);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaximumFileBytes)
        {
            throw new ModelRootRefused($"'{path}' exceeds {MaximumFileBytes} bytes.");
        }

        var content = new byte[checked((int)stream.Length)];
        stream.ReadExactly(content);
        if (stream.ReadByte() != -1)
        {
            throw new ModelRootRefused($"'{path}' changed while being read.");
        }

        CheckAncestors(path);
        return content;
    }

    static bool Present(string path)
    {
        try
        {
            _ = File.GetAttributes(path);
            return true;
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            // Absent: the caller decides whether this level may be created later.
            return false;
        }
    }

    void CheckParents(string[] parents, bool create)
    {
        if (create)
        {
            Create();
        }

        var current = _path;
        foreach (var segment in parents)
        {
            current = Path.Combine(current, segment);
            ModelRootMetadata.CheckExisting(current);
            if (Directory.Exists(current) || File.Exists(current))
            {
                CheckAncestors(current);
                if (!Directory.Exists(current))
                {
                    throw new ModelRootRefused($"A file blocks directory '{current}'.");
                }
            }
            else if (create)
            {
                Directory.CreateDirectory(current);
                CheckAncestors(current);
            }
        }
    }

    // A root that does not exist yet reads as an empty model, so traversal starts with nothing to visit.
    Stack<(string Path, int Depth)> TraversalStart()
    {
        var directories = new Stack<(string Path, int Depth)>();
        if (Exists)
        {
            directories.Push((_path, 0));
        }

        return directories;
    }

    void CheckDirectory()
    {
        var existing = _path;
        for (var missing = 0; !Present(existing); missing++)
        {
            var parent = Path.GetDirectoryName(existing);
            if (parent is null || missing >= _missingLevels)
            {
                throw new ModelRootRefused("The MCP root must be a directory, or a missing directory whose parent exists.");
            }

            existing = parent;
        }

        CheckAncestors(existing);
        if (!Directory.Exists(existing))
        {
            throw new ModelRootRefused("The MCP root must be a directory, or a missing directory whose parent exists.");
        }
    }
}
