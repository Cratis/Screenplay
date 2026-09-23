// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.AccessControl;

namespace Cratis.Screenplay.Mcp;

sealed record McpRecoveryAccess(string Path, string Rules)
{
    internal static McpRecoveryAccess Capture(string relative, string path) => new(relative, OperatingSystem.IsWindows()
        ? new FileInfo(path).GetAccessControl(AccessControlSections.Access).GetSecurityDescriptorSddlForm(AccessControlSections.Access)
        : ((int)File.GetUnixFileMode(path)).ToString(CultureInfo.InvariantCulture));

    internal void Validate()
    {
        if (Rules.Length > 65536)
        {
            throw new McpFailure("Recovery access rules exceed the bounded descriptor size.");
        }

        if (OperatingSystem.IsWindows())
        {
            var access = new FileSecurity();
            access.SetSecurityDescriptorSddlForm(Rules, AccessControlSections.Access);
        }
        else if (!int.TryParse(Rules, NumberStyles.None, CultureInfo.InvariantCulture, out var mode) || mode < 0 || (mode & ~4095) != 0)
        {
            throw new McpFailure("Recovery access rules contain an invalid Unix mode.");
        }
    }

    internal void Verify(string path)
    {
        McpManagedFiles.CheckExisting(path);
        if (Capture(Path, path).Rules != Rules)
        {
            throw new McpFailure($"RecoveryAccessConflict: original access rules were not restored for '{Path}'.");
        }
    }

    internal void Restore(string path)
    {
        Validate();
        if (OperatingSystem.IsWindows())
        {
            var access = new FileSecurity();
            access.SetSecurityDescriptorSddlForm(Rules, AccessControlSections.Access);
            new FileInfo(path).SetAccessControl(access);
        }
        else
        {
            File.SetUnixFileMode(path, (UnixFileMode)int.Parse(Rules, CultureInfo.InvariantCulture));
        }
    }
}
