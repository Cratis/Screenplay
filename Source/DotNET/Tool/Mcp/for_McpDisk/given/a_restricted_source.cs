// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.AccessControl;
using System.Security.Principal;

namespace Cratis.Screenplay.Tool.Mcp.for_McpDisk.given;

public class a_restricted_source : for_McpConnection.given.a_connection
{
    protected string OriginalAccess = string.Empty;

    void Establish()
    {
        var path = Path.Combine(RootPath, "application.play");
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            var access = new FileSecurity();
            access.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            access.AddAccessRule(new FileSystemAccessRule(identity.User, FileSystemRights.FullControl, AccessControlType.Allow));
            new FileInfo(path).SetAccessControl(access);
        }
        else
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        OriginalAccess = Access(path);
    }

    protected static string Access(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return new FileInfo(path).GetAccessControl(AccessControlSections.Access).GetSecurityDescriptorSddlForm(AccessControlSections.Access);
        }

        return File.GetUnixFileMode(path).ToString();
    }
}
