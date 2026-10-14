// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Cratis.Screenplay.Mcp;

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
