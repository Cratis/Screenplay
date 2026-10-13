// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Bounded reads of the private <c>.screenplay</c> metadata directory, shared by the MCP server and <see cref="ScreenplayModelRoot"/>.
/// </summary>
static class ModelRootMetadata
{
    internal const string PendingOperationFileName = "pending.json";
    internal const int MaximumJournalBytes = 64 * 1024 * 1024;

    internal static void CheckExisting(string path)
    {
        try
        {
            if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new ModelRootRefused($"MetadataPathConflict: symbolic links and reparse points are not admitted: '{path}'.");
            }

            ModelRootDirectory.CheckAncestors(path);
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
            throw new ModelRootRefused($"FileConflict: a directory occupies '{path}'.");
        }

        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > maximum)
        {
            throw new ModelRootRefused($"'{path}' exceeds the {maximum}-byte bound.");
        }

        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1)
        {
            throw new ModelRootRefused($"DiskDrift: '{path}' changed while being read.");
        }

        CheckExisting(path);
        return bytes;
    }

    // createDirectory runs only when the directory is absent; read-only callers pass null and never create it.
    internal static string PathFor(ModelRootDirectory root, string name, Action<string>? createDirectory = null)
    {
        if (name.Length == 0 || name == "." || name == ".." || name.Length > 128 || name.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '-'))
        {
            throw new ModelRootRefused("Invalid metadata filename.");
        }

        var directory = Path.Combine(root.DirectoryPath, ".screenplay");
        CheckExisting(directory);
        if (createDirectory is not null && !Directory.Exists(directory))
        {
            createDirectory(directory);
        }

        if (Directory.Exists(directory))
        {
            ModelRootDirectory.CheckAncestors(directory);
            VerifyPrivateDirectory(directory);
        }
        else if (File.Exists(directory))
        {
            throw new ModelRootRefused("ReservedPath: .screenplay must be a private metadata directory.");
        }

        var path = Path.Combine(directory, name);
        CheckExisting(path);
        return path;
    }

    internal static byte[]? Read(ModelRootDirectory root, string name, int maximum) => ReadPath(PathFor(root, name), maximum);

    internal static void RefusePending(ModelRootDirectory root)
    {
        if (Read(root, PendingOperationFileName, MaximumJournalBytes) is not null)
        {
            throw new ModelRootRefused(
                "PendingOperation: .screenplay/pending.json records an interrupted or uncertain apply. Use workspace-state, then explicitly recover-workspace with its operationId before opening or editing.",
                "PendingOperation") { McpFailureKind = "PendingOperation" };
        }
    }

    internal static void VerifyPrivateDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            VerifyWindowsDirectory(path);
        }
        else if ((File.GetUnixFileMode(path) &
            (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
             UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != UnixFileMode.None)
        {
            throw new ModelRootRefused("MetadataPermissions: make .screenplay accessible only to its owner before continuing.", "MetadataPermissions");
        }
    }

    [SupportedOSPlatform("windows")]
    static void VerifyWindowsDirectory(string path)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var access = new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access);
        if (!access.AreAccessRulesProtected || access.GetAccessRules(true, true, typeof(SecurityIdentifier)).OfType<FileSystemAccessRule>()
            .Any(rule => rule.AccessControlType == AccessControlType.Allow && !rule.IdentityReference.Equals(identity.User)))
        {
            throw new ModelRootRefused("MetadataPermissions: .screenplay must have protected access rules granting access only to the current Windows user.", "MetadataPermissions");
        }
    }
}
