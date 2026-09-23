// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

sealed class McpManagedFiles(McpRoot root)
{
    internal const int MaximumStateBytes = McpRoot.MaximumBytes;
    internal const int MaximumJournalBytes = 64 * 1024 * 1024;

    internal static bool Equal(byte[]? left, byte[]? right) => left is null ? right is null : right is not null && left.AsSpan().SequenceEqual(right);

    internal static void CheckExisting(string path)
    {
        try
        {
            if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new McpFailure($"MetadataPathConflict: symbolic links and reparse points are not admitted: '{path}'.");
            }

            McpRoot.CheckAncestors(path);
        }
        catch (FileNotFoundException)
        {
            // Absence is a valid preimage; it never authorizes following a reparse point.
        }
        catch (DirectoryNotFoundException)
        {
            // The metadata directory need not exist for a read-only inspection.
        }
    }

    internal static byte[]? ReadPath(string path, int maximum)
    {
        CheckExisting(path);
        if (Directory.Exists(path))
        {
            throw new McpFailure($"FileConflict: a directory occupies '{path}'.");
        }

        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > maximum)
        {
            throw new McpFailure($"'{path}' exceeds the {maximum}-byte bound.");
        }

        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1)
        {
            throw new McpFailure($"DiskDrift: '{path}' changed while being read.");
        }

        CheckExisting(path);
        return bytes;
    }

    internal static void WritePrivate(string path, byte[] bytes)
    {
        CheckExisting(path);
        using var stream = McpFileAccess.CreatePrivate(path);
        stream.Write(bytes);
        stream.Flush(true);
    }

    internal string PathFor(string name, bool create = false)
    {
        if (name.Length == 0 || name == "." || name == ".." || name.Length > 128 || name.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '-'))
        {
            throw new McpFailure("Invalid metadata filename.");
        }

        var directory = Path.Combine(root.DirectoryPath, ".screenplay");
        CheckExisting(directory);
        if (create && !Directory.Exists(directory))
        {
            McpFileAccess.CreatePrivateDirectory(directory);
        }

        if (Directory.Exists(directory))
        {
            McpRoot.CheckAncestors(directory);
            McpFileAccess.VerifyPrivateDirectory(directory);
        }
        else if (File.Exists(directory))
        {
            throw new McpFailure("ReservedPath: .screenplay must be a private metadata directory.");
        }

        var path = Path.Combine(directory, name);
        CheckExisting(path);
        return path;
    }

    internal byte[]? Read(string name, int maximum = MaximumStateBytes) => ReadPath(PathFor(name), maximum);

    internal void Verify(string name, byte[]? expected)
    {
        if (!Equal(Read(name), expected))
        {
            throw new McpFailure($"IdentityStateDrift: '.screenplay/{name}' changed; reopen before proposing changes.");
        }
    }
}
