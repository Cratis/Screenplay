// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed class McpManagedFiles(McpRoot root)
{
    internal const int MaximumStateBytes = McpRoot.MaximumBytes;
    internal const int MaximumJournalBytes = ModelRootMetadata.MaximumJournalBytes;

    internal static bool Equal(byte[]? left, byte[]? right) => left is null ? right is null : right is not null && left.AsSpan().SequenceEqual(right);

    internal static void CheckExisting(string path) => McpRootFailures.Run(() => ModelRootMetadata.CheckExisting(path));

    internal static byte[]? ReadPath(string path, int maximum) => McpRootFailures.Run(() => ModelRootMetadata.ReadPath(path, maximum));

    internal static void WritePrivate(string path, byte[] bytes)
    {
        CheckExisting(path);
        var directory = Path.GetDirectoryName(path)!;
        if (Path.GetFileName(directory).Equals(".screenplay", StringComparison.OrdinalIgnoreCase))
        {
            McpRootFailures.Run(() => ModelRootMetadata.VerifyDirectory(directory, ModelRootMetadataAccess.Write));
        }

        using var stream = McpFileAccess.CreatePrivate(path);
        stream.Write(bytes);
        stream.Flush(true);
    }

    internal string PathFor(string name, bool create = false) =>
        McpRootFailures.Run(() => ModelRootMetadata.PathFor(root.Directory, name, ModelRootMetadataAccess.Write, create ? CreateDirectory : null));

    internal byte[]? Read(string name, int maximum = MaximumStateBytes, ModelRootMetadataAccess access = ModelRootMetadataAccess.Read) =>
        ReadPath(McpRootFailures.Run(() => ModelRootMetadata.PathFor(root.Directory, name, access)), maximum);

    internal void Verify(string name, byte[]? expected)
    {
        if (!Equal(Read(name), expected))
        {
            var kind = DriftKind(name);
            throw new McpFailure($"{kind}: '.screenplay/{name}' changed; reopen before proposing changes.") { FailureKind = kind };
        }
    }

    static string DriftKind(string name) => name == ModelNotesStore.FileName ? "NotesRevisionDrift" : "IdentityStateDrift";

    void CreateDirectory(string directory)
    {
        root.Create();
        McpFileAccess.CreatePrivateDirectory(directory);
    }
}
