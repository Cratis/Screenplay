// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Mcp;

// Git's linked-worktree registration, not path spelling, grants access to another checkout.
internal static class McpWorktreeRoots
{
    const int MaximumMetadataBytes = 8192;
    const int MaximumConfigurationLineBytes = 64 * 1024;
    const int MaximumConfigurationBytes = 1024 * 1024;
    static readonly UTF8Encoding _strictUtf8 = new(false, true);

    internal static McpRoot Resolve(McpRoot configured, McpRoot requested)
    {
        try
        {
            return ResolveRegistered(configured, requested);
        }
        catch (McpFailure failure) when (failure.FailureKind != "RootChangeRefused")
        {
            throw Refused($"Cannot verify the requested Git worktree: {failure.Message}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw Refused($"Cannot verify the requested Git worktree: {exception.Message}");
        }
    }

    static McpRoot ResolveRegistered(McpRoot configured, McpRoot requested)
    {
        var approved = Locate(configured);
        var candidate = Locate(requested);
        if (approved is null || candidate is null || !McpDirectoryIdentity.Same(approved.Common, candidate.Common))
        {
            throw Refused();
        }

        var relative = Path.GetRelativePath(approved.Checkout.DirectoryPath, configured.DirectoryPath);
        var modelPath = Path.Combine(candidate.Checkout.DirectoryPath, relative);
        if (!Directory.Exists(modelPath))
        {
            throw Refused($"The worktree has no '{relative}' model folder.");
        }

        var model = new McpRoot(modelPath);
        if (!McpDirectoryIdentity.Same(requested, candidate.Checkout) && !McpDirectoryIdentity.Same(requested, model))
        {
            throw Refused();
        }

        return model;
    }

    static Registration? Locate(McpRoot root)
    {
        for (var directory = root.DirectoryPath; directory is not null; directory = Path.GetDirectoryName(directory))
        {
            var marker = Path.Combine(directory, ".git");
            if (!Directory.Exists(marker) && !File.Exists(marker))
            {
                continue;
            }

            McpRoot.CheckAncestors(marker);
            var checkout = new McpRoot(directory);
            if (Directory.Exists(marker))
            {
                RequireNonBare(directory);

                return new(checkout, new McpRoot(marker));
            }

            return Linked(checkout, marker);
        }

        return null;
    }

    static Registration Linked(McpRoot checkout, string marker)
    {
        var pointer = Read(marker);
        if (!pointer.StartsWith("gitdir: ", StringComparison.Ordinal))
        {
            throw Refused();
        }

        var gitDirectory = new McpRoot(Path.GetFullPath(pointer[8..], checkout.DirectoryPath));
        var parent = Parent(gitDirectory.DirectoryPath);
        var commonPath = Path.Combine(gitDirectory.DirectoryPath, "commondir");
        if (!File.Exists(commonPath))
        {
            throw Refused("Git metadata has no commondir; submodules and separate-git-dir checkouts cannot switch roots.");
        }

        var common = new McpRoot(Path.GetFullPath(Read(commonPath), gitDirectory.DirectoryPath));
        var registrations = new McpRoot(Path.Combine(common.DirectoryPath, "worktrees"));
        var backPointer = Path.GetFullPath(Read(Path.Combine(gitDirectory.DirectoryPath, "gitdir")), gitDirectory.DirectoryPath);
        var registeredCheckout = Parent(backPointer);
        if (!McpDirectoryIdentity.Same(parent, registrations) || !McpDirectoryIdentity.Same(checkout, registeredCheckout) ||
            !McpDirectoryIdentity.SameEntry(marker, backPointer))
        {
            throw Refused();
        }

        return new(checkout, common);
    }

    static McpRoot Parent(string path) => Path.GetDirectoryName(path) is { } parent
        ? new McpRoot(parent) : throw Refused("Git registration must name a directory or file beneath a filesystem root.");

    static void RequireNonBare(string checkout)
    {
        bool bare;
        try
        {
            bare = ReadBareStatus(checkout);
        }
        catch (McpFailure failure)
        {
            throw ConfigurationRefused(checkout, failure.Message);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw ConfigurationRefused(checkout, exception.Message);
        }

        if (bare)
        {
            throw Refused();
        }
    }

    static bool ReadBareStatus(string checkout)
    {
        var gitDirectory = Path.Combine(checkout, ".git");
        RequireNoWorktreeConfiguration(gitDirectory);
        var core = false;
        var extensions = false;
        var seenCore = false;
        var seenSection = false;
        var firstLine = true;
        bool? bare = null;
        foreach (var rawLine in ReadConfigurationLines(Path.Combine(gitDirectory, "config")))
        {
            var line = TrimConfigurationWhitespace(firstLine && rawLine.StartsWith('\uFEFF') ? rawLine[1..] : rawLine);
            firstLine = false;
            if (line.Length == 0 || line[0] is '#' or ';')
            {
                continue;
            }

            if (line.EndsWith('\\'))
            {
                throw Refused("Configuration continuations are not supported.");
            }

            if (line[0] == '[')
            {
                var section = ReadConfigurationSection(line);

                // Includes could override core.bare outside this bounded config; do not guess their outcome.
                if (section.Equals("include", StringComparison.OrdinalIgnoreCase) || section.StartsWith("includeIf", StringComparison.OrdinalIgnoreCase))
                {
                    throw Refused("Configuration includes are not supported.");
                }

                core = section.Equals("core", StringComparison.OrdinalIgnoreCase);
                extensions = section.Equals("extensions", StringComparison.OrdinalIgnoreCase);
                if (core && seenCore)
                {
                    throw Refused("The [core] section is declared more than once.");
                }
                seenCore |= core;
                seenSection = true;
                continue;
            }

            if (!seenSection)
            {
                throw Refused("A configuration entry has no section.");
            }

            if (!core && !extensions)
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator < 0)
            {
                throw Refused($"A [{(core ? "core" : "extensions")}] entry has no explicit value.");
            }

            var key = TrimConfigurationWhitespace(line[..separator]);
            if (key.Length == 0 || !char.IsAsciiLetter(key[0]) || key.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
            {
                throw Refused("A configuration entry name is malformed.");
            }

            var value = TrimConfigurationWhitespace(line[(separator + 1)..]).ToLowerInvariant();
            if (extensions && key.Equals("worktreeConfig", StringComparison.OrdinalIgnoreCase) && value is not ("false" or "no" or "off" or "0" or ""))
            {
                throw Refused("Per-worktree configuration is not supported.");
            }

            if (core && key.Equals("bare", StringComparison.OrdinalIgnoreCase))
            {
                if (bare is not null)
                {
                    throw Refused("core.bare is declared more than once.");
                }

                bare = value switch
                {
                    "false" => false,
                    "true" => true,
                    _ => throw Refused("core.bare must be explicitly true or false.")
                };
            }
        }

        if (bare is { } declared)
        {
            return declared;
        }

        // This branch is only reached for the checkout's directory marker, never a linked worktree's common directory.
        var index = Path.Combine(gitDirectory, "index");
        if (!Directory.Exists(gitDirectory) || !File.Exists(index))
        {
            throw Refused("core.bare is absent and no working tree index exists.");
        }

        McpRoot.CheckAncestors(index);
        if (!McpDirectoryIdentity.IsRegularFile(index))
        {
            throw Refused("core.bare is absent and the working tree index is not a regular file.");
        }

        return false;
    }

    static void RequireNoWorktreeConfiguration(string gitDirectory)
    {
        RequireAbsent(gitDirectory, "config.worktree", "Per-worktree configuration is not supported.");

        // Git reads the configuration of the common directory a commondir file points to, not this one.
        RequireAbsent(gitDirectory, "commondir", "A common-directory redirect is not supported.");
    }

    static void RequireAbsent(string gitDirectory, string name, string reason)
    {
        try
        {
            _ = File.GetAttributes(Path.Combine(gitDirectory, name));
        }
        catch (FileNotFoundException)
        {
            // Only a missing entry proves that this source cannot decide core.bare.
            return;
        }

        throw Refused(reason);
    }

    static string TrimConfigurationWhitespace(string value) => value.Trim(' ', '\t', '\r');

    static string ReadConfigurationSection(string line)
    {
        var index = 1;
        while (index < line.Length && (char.IsAsciiLetterOrDigit(line[index]) || line[index] is '.' or '-'))
        {
            index++;
        }

        if (index == 1)
        {
            throw Refused("A configuration section is malformed.");
        }

        if (index < line.Length && line[index] is ' ' or '\t')
        {
            if (++index == line.Length || line[index++] != '"')
            {
                throw Refused("A configuration section is malformed.");
            }

            while (index < line.Length && line[index] != '"')
            {
                if (line[index] == '\\' && (++index == line.Length || line[index] is not ('"' or '\\')))
                {
                    throw Refused("A configuration subsection escape is malformed.");
                }
                index++;
            }
            if (index == line.Length)
            {
                throw Refused("A configuration section is malformed.");
            }
            index++;
        }

        if (index == line.Length || line[index] != ']')
        {
            throw Refused("A configuration section is malformed.");
        }

        var remainder = TrimConfigurationWhitespace(line[(index + 1)..]);
        if (remainder.Length > 0 && remainder[0] is not ('#' or ';'))
        {
            throw Refused("A configuration section header must be followed only by a comment or whitespace.");
        }

        return line[1..index];
    }

    static IEnumerable<string> ReadConfigurationLines(string path)
    {
        McpRoot.CheckAncestors(path);
        if (!McpDirectoryIdentity.IsRegularFile(path))
        {
            throw Refused("The configuration is not a regular file.");
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaximumConfigurationBytes)
        {
            throw Refused($"Configuration exceeds the {MaximumConfigurationBytes}-byte total limit.");
        }

        var bytes = new byte[MaximumConfigurationLineBytes];
        var count = 0;
        var total = 0;
        while (true)
        {
            var value = stream.ReadByte();
            if (value == -1)
            {
                if (count > 0)
                {
                    yield return _strictUtf8.GetString(bytes, 0, count);
                }
                break;
            }

            if (++total > MaximumConfigurationBytes)
            {
                throw Refused($"Configuration exceeds the {MaximumConfigurationBytes}-byte total limit.");
            }

            if (value == 0)
            {
                throw Refused("Configuration contains a null byte.");
            }

            if (value == '\n')
            {
                yield return _strictUtf8.GetString(bytes, 0, count);
                count = 0;
                continue;
            }

            if (count == bytes.Length)
            {
                throw Refused($"A configuration line exceeds the {MaximumConfigurationLineBytes}-byte limit.");
            }
            bytes[count++] = (byte)value;
        }

        McpRoot.CheckAncestors(path);
    }

    static McpFailure ConfigurationRefused(string checkout, string reason) => Refused($"Cannot determine whether {checkout} is a bare repository: {reason}");

    static string Read(string path)
    {
        var value = ReadMetadata(path);
        if (value.Contains('\n') || value.Contains('\r'))
        {
            throw Refused();
        }

        return value;
    }

    static string ReadMetadata(string path)
    {
        McpRoot.CheckAncestors(path);
        if (!McpDirectoryIdentity.IsRegularFile(path))
        {
            throw Refused();
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is 0 or > MaximumMetadataBytes)
        {
            throw Refused();
        }

        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1)
        {
            throw Refused();
        }

        McpRoot.CheckAncestors(path);
        var value = _strictUtf8.GetString(bytes).TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(value) || value.Contains('\0'))
        {
            throw Refused();
        }

        return value;
    }

    static McpFailure Refused(string? reason = null) => new(reason ?? "The requested root must be the corresponding model directory in a registered worktree of the configured repository.") { FailureKind = "RootChangeRefused" };

    sealed record Registration(McpRoot Checkout, McpRoot Common);
}
