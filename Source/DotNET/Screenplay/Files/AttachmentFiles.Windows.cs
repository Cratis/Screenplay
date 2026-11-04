// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Cratis.Screenplay.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Screenplay.Files;

public static partial class AttachmentFiles
{
    const uint WindowsListDirectory = 0x1;
    const uint WindowsTraverse = 0x20;
    const uint WindowsReadAttributes = 0x80;
    const uint WindowsGenericRead = 0x80000000;
    const uint WindowsShareRead = 0x1;
    const uint WindowsShareWrite = 0x2;
    const uint WindowsOpenExisting = 3;
    const uint WindowsOpenReparsePoint = 0x00200000;
    const uint WindowsBackupSemantics = 0x02000000;
    const uint WindowsFileTypeDisk = 1;
    const int WindowsFileAttributeTagInfo = 9;

    internal static bool WindowsAttachmentMatchesPath(string rootFinalPath, string key, string attachmentFinalPath) =>
        string.Equals(NormalizeWindowsFinalPath(attachmentFinalPath), NormalizeWindowsFinalPath(rootFinalPath) + '\\' + key.Replace('/', '\\'), StringComparison.OrdinalIgnoreCase);

    internal static string WindowsAttachmentRefusalCode(bool reparsePoint, bool matchesPath) =>
        (reparsePoint, matchesPath) switch
        {
            (true, _) => DiagnosticCodes.AttachmentLinkRefused,
            (false, false) => DiagnosticCodes.AttachmentPathRefused,
            _ => DiagnosticCodes.AttachmentUnreadable
        };

    static string NormalizeWindowsFinalPath(string path)
    {
        var normalized = path.Replace('/', '\\').TrimEnd('\\');
        if (normalized.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase)) return @"\\" + normalized[8..];
        return normalized.StartsWith(@"\\?\", StringComparison.Ordinal) ? normalized[4..] : normalized;
    }

    static SafeFileHandle? OpenWindowsRoot(string root, out string? finalPath, out bool linkRefused)
    {
        finalPath = null;
        linkRefused = false;
        if (!OperatingSystem.IsWindows()) return null;
        SafeFileHandle? handle = null;
        try
        {
            // Directory list/traverse access participates in share checks; omitting delete sharing prevents renaming or deleting the held root.
            handle = CreateWindowsFile(root, WindowsListDirectory | WindowsTraverse | WindowsReadAttributes, WindowsShareRead | WindowsShareWrite, 0, WindowsOpenExisting, WindowsBackupSemantics | WindowsOpenReparsePoint, 0);
            if (handle.IsInvalid || !TryReadWindowsAttributes(handle, out var attributes)) return null;
            linkRefused = attributes.HasFlag(FileAttributes.ReparsePoint);
            if (linkRefused) return null;
            finalPath = WindowsFinalPath(handle);
            if (finalPath is null) return null;
            var owned = handle;
            handle = null;
            return owned;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            return null;
        }
        finally
        {
            handle?.Dispose();
        }
    }

    static FileStream? OpenWindowsAttachment(string rootFinalPath, string path, string key, out string reason, out string refusalCode)
    {
        reason = "cannot be opened";
        refusalCode = DiagnosticCodes.AttachmentUnreadable;
        if (!OperatingSystem.IsWindows()) return null;
        SafeFileHandle? handle = null;
        try
        {
            handle = CreateWindowsFile(path, WindowsGenericRead, WindowsShareRead, 0, WindowsOpenExisting, WindowsOpenReparsePoint, 0);
            if (handle.IsInvalid) return null;
            if (!VerifyWindowsAttachment(handle, rootFinalPath, key, out reason, out refusalCode)) return null;
            var stream = new FileStream(handle, FileAccess.Read);
            handle = null; // The stream owns the verified attachment handle from here on.
            return stream;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            reason = "cannot verify the file type on this platform";
            return null;
        }
        finally
        {
            handle?.Dispose();
        }
    }

    static bool VerifyWindowsAttachment(SafeFileHandle handle, string rootFinalPath, string key, out string reason, out string refusalCode)
    {
        reason = "is not a regular file";
        refusalCode = WindowsAttachmentRefusalCode(false, true);
        if (GetWindowsFileType(handle) != WindowsFileTypeDisk) return false;
        reason = "cannot be inspected";
        if (!TryReadWindowsAttributes(handle, out var attributes)) return false;
        if (attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            reason = "crosses a symbolic link or reparse point";
            refusalCode = WindowsAttachmentRefusalCode(true, false);
            return false;
        }

        reason = "is a directory";
        if (attributes.HasFlag(FileAttributes.Directory)) return false;
        reason = "cannot be inspected";
        var finalPath = WindowsFinalPath(handle);
        if (finalPath is null) return false;
        var matchesPath = WindowsAttachmentMatchesPath(rootFinalPath, key, finalPath);
        refusalCode = WindowsAttachmentRefusalCode(false, matchesPath);
        reason = "does not match its name on disk; short (8.3) names and trailing dots or spaces are not supported";
        return matchesPath;
    }

    static bool TryReadWindowsAttributes(SafeFileHandle handle, out FileAttributes attributes)
    {
        var succeeded = GetWindowsFileInformation(handle, WindowsFileAttributeTagInfo, out var information, (uint)Marshal.SizeOf<WindowsAttributeTagInformation>());
        attributes = (FileAttributes)information.Attributes;
        return succeeded;
    }

    static string? WindowsFinalPath(SafeFileHandle handle)
    {
        // Zero selects FILE_NAME_NORMALIZED | VOLUME_NAME_DOS for both root and attachment.
        var buffer = new char[512];
        var length = GetWindowsFinalPath(handle, buffer, (uint)buffer.Length, 0);
        if (length >= buffer.Length)
        {
            buffer = new char[checked((int)length)];
            length = GetWindowsFinalPath(handle, buffer, (uint)buffer.Length, 0);
        }

        return length == 0 || length >= buffer.Length ? null : new string(buffer, 0, (int)length);
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial SafeFileHandle CreateWindowsFile(string path, uint access, uint share, nint securityAttributes, uint creation, uint flags, nint template);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", EntryPoint = "GetFileInformationByHandleEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowsFileInformation(SafeFileHandle handle, int informationClass, out WindowsAttributeTagInformation information, uint size);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", EntryPoint = "GetFileType", SetLastError = true)]
    private static partial uint GetWindowsFileType(SafeFileHandle handle);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint GetWindowsFinalPath(SafeFileHandle handle, [Out] char[] path, uint length, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    struct WindowsAttributeTagInformation
    {
        internal uint Attributes;
        internal uint ReparseTag;
    }
}
