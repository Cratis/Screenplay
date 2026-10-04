// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Screenplay.Mcp;

// Physical identity, not an OS-wide case rule. Callers must admit both paths through McpRoot first.
internal static partial class McpDirectoryIdentity
{
    internal static bool Same(McpRoot approved, McpRoot requested)
    {
        var before = Read(approved.DirectoryPath);
        var candidate = Read(requested.DirectoryPath);
        McpRoot.CheckAncestors(approved.DirectoryPath);
        McpRoot.CheckAncestors(requested.DirectoryPath);
        return before == candidate;
    }

    // Metadata-only identity also admits existing hard links and filesystem-resolved case aliases.
    internal static bool SameEntry(string first, string second)
    {
        McpRoot.CheckAncestors(first);
        McpRoot.CheckAncestors(second);
        var same = Read(first) == Read(second);
        McpRoot.CheckAncestors(first);
        McpRoot.CheckAncestors(second);

        return same;
    }

    static Identity Read(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                // FILE_FLAG_BACKUP_SEMANTICS opens a directory; OPEN_REPARSE_POINT never follows a final link.
                using var handle = OpenDirectory(path, 0, 7, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
                var information = new byte[24]; // FILE_ID_INFO: volume serial (64 bits) and file ID (128 bits).
                if (handle.IsInvalid || !FileIdentity(handle, 18, information, (uint)information.Length))
                {
                    throw Unavailable(path, Marshal.GetLastPInvokeError().ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                return new(
                    BinaryPrimitives.ReadUInt64LittleEndian(information),
                    BinaryPrimitives.ReadUInt64LittleEndian(information.AsSpan(8)),
                    BinaryPrimitives.ReadUInt64LittleEndian(information.AsSpan(16)));
            }

            var architecture = RuntimeInformation.ProcessArchitecture;
            if ((!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux()) || architecture is not (Architecture.X64 or Architecture.Arm64))
            {
                throw Unavailable(path, "unsupported directory-identity ABI");
            }

            // Darwin's 64-bit-inode stat starts with dev_t (32 bits), mode/nlink, then ino_t (64 bits).
            // Linux x64/arm64 stat starts with dev_t and ino_t (both 64 bits). No guessed offsets on other ABIs.
            var status = new byte[512];
            var result = OperatingSystem.IsMacOS() && architecture == Architecture.X64 ? LStatMacX64(path, status) : LStat(path, status);
            if (result != 0)
            {
                throw Unavailable(path, Marshal.GetLastPInvokeError().ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            return new(
                OperatingSystem.IsMacOS() ? BinaryPrimitives.ReadUInt32LittleEndian(status) : BinaryPrimitives.ReadUInt64LittleEndian(status),
                BinaryPrimitives.ReadUInt64LittleEndian(status.AsSpan(8)),
                0);
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            throw Unavailable(path, exception.Message);
        }
    }

    static McpFailure Unavailable(string path, string reason) => new($"Cannot establish physical directory identity for '{path}': {reason}.") { FailureKind = "RootChangeRefused" };

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("libc", EntryPoint = "lstat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int LStat(string path, [Out] byte[] status);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("libc", EntryPoint = "lstat$INODE64", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int LStatMacX64(string path, [Out] byte[] status);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial SafeFileHandle OpenDirectory(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", EntryPoint = "GetFileInformationByHandleEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FileIdentity(SafeFileHandle handle, int informationClass, [Out] byte[] information, uint size);

    [StructLayout(LayoutKind.Auto)]
    readonly record struct Identity(ulong Volume, ulong File, ulong FileHigh);
}
