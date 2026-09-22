// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpFileAccess
{
    internal static FileStream CreatePrivate(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateWindows(path);
        }

        return new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
        });
    }

    internal static void CreatePrivateDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            CreateWindowsDirectory(path);
        }
        else
        {
            Directory.CreateDirectory(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
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
            throw new McpFailure("MetadataPermissions: make .screenplay accessible only to its owner before continuing.");
        }
    }

    internal static void Preserve(string original, string staged)
    {
        if (OperatingSystem.IsWindows())
        {
            var access = new FileInfo(original).GetAccessControl(AccessControlSections.Access);
            new FileInfo(staged).SetAccessControl(access);
        }
        else
        {
            File.SetUnixFileMode(staged, File.GetUnixFileMode(original));
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
            throw new McpFailure("MetadataPermissions: .screenplay must have protected access rules granting access only to the current Windows user.");
        }
    }

    [SupportedOSPlatform("windows")]
    static void CreateWindowsDirectory(string path)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var user = identity.User ?? throw new McpFailure("The current Windows identity has no user SID for private metadata.");
        var access = new DirectorySecurity();
        access.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        access.AddAccessRule(new FileSystemAccessRule(
            user,
            FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));
        new DirectoryInfo(path).Create(access);
    }

    [SupportedOSPlatform("windows")]
    static FileStream CreateWindows(string path)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var user = identity.User ?? throw new McpFailure("The current Windows identity has no user SID for private file staging.");
        var access = new FileSecurity();
        access.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        access.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Allow));
        return new FileInfo(path).Create(FileMode.CreateNew, FileSystemRights.FullControl, FileShare.None, 4096, FileOptions.None, access);
    }
}
