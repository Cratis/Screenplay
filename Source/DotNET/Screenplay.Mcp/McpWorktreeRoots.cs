// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Mcp;

// Git's linked-worktree registration, not path spelling, grants access to another checkout.
internal static class McpWorktreeRoots
{
    const int MaximumMetadataBytes = 8192;
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

    static string Read(string path)
    {
        McpRoot.CheckAncestors(path);
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
        if (string.IsNullOrWhiteSpace(value) || value.Contains('\n') || value.Contains('\r') || value.Contains('\0'))
        {
            throw Refused();
        }

        return value;
    }

    static McpFailure Refused(string? reason = null) => new(reason ?? "The requested root must be the corresponding model directory in a registered worktree of the configured repository.") { FailureKind = "RootChangeRefused" };

    sealed record Registration(McpRoot Checkout, McpRoot Common);
}
