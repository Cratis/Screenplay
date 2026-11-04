// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Screenplay.Files;

public static partial class AttachmentFiles
{
    // Linux: asm-generic/fcntl.h, with the arm64 asm/fcntl.h overrides.
    // Darwin: sys/fcntl.h. O_RDONLY is zero on all four supported ABIs.
    static (int Directory, int NoFollow, int CloseOnExec, int NonBlock)? UnixOpenFlags()
    {
        if (!BitConverter.IsLittleEndian || RuntimeInformation.ProcessArchitecture is not (Architecture.X64 or Architecture.Arm64))
        {
            return null;
        }

        if (OperatingSystem.IsMacOS()) return (0x100000, 0x100, 0x1000000, 0x4);
        if (!OperatingSystem.IsLinux()) return null;
        return RuntimeInformation.ProcessArchitecture == Architecture.X64
            ? (0x10000, 0x20000, 0x80000, 0x800)
            : (0x4000, 0x8000, 0x80000, 0x800);
    }

    static SafeFileHandle? OpenUnixRoot(string root)
    {
        if (UnixOpenFlags() is not { } flags) return null;
        try
        {
            return OwnUnixDescriptor(Open(root, flags.Directory | flags.NoFollow | flags.CloseOnExec));
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            return null;
        }
    }

    static SafeFileHandle? OwnUnixDescriptor(int descriptor) => descriptor < 0 ? null : new SafeFileHandle(descriptor, ownsHandle: true);

    static SafeFileHandle? OpenUnixRelative(SafeFileHandle root, string key)
    {
        var flags = UnixOpenFlags()!.Value;
        var segments = key.Split('/');
        SafeFileHandle? directory = null;
        try
        {
            for (var index = 0; index < segments.Length - 1; index++)
            {
                var next = OwnUnixDescriptor(OpenAt(directory ?? root, segments[index], flags.Directory | flags.NoFollow | flags.CloseOnExec));
                directory?.Dispose();
                directory = next;
                if (directory is null) return null;
            }

            return OwnUnixDescriptor(OpenAt(directory ?? root, segments[^1], flags.NoFollow | flags.CloseOnExec | flags.NonBlock));
        }
        finally
        {
            directory?.Dispose();
        }
    }

    static FileStream? OpenUnixAttachment(SafeFileHandle? root, string key, out long length, out string reason)
    {
        length = 0;
        reason = "cannot verify the file type on this platform";
        if (root is null) return null;
        reason = "cannot be opened";
        SafeFileHandle? handle = null;
        try
        {
            handle = OpenUnixRelative(root, key);
            if (handle is null) return null;
            if (!TryReadUnixStatus(handle, out length, out reason)) return null;
            var stream = new FileStream(handle, FileAccess.Read);
            handle = null; // The stream owns the final descriptor from here on.
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

    static bool TryReadUnixStatus(SafeFileHandle handle, out long length, out string reason)
    {
        length = 0;
        reason = "cannot verify the file type on this platform";
        var status = new byte[512];
        var mac = OperatingSystem.IsMacOS();
        var x64 = RuntimeInformation.ProcessArchitecture == Architecture.X64;
        var result = mac && x64 ? FStatMacX64(handle, status) : FStat(handle, status);
        if (result != 0) return false;

        // Darwin stat64: mode at 4, size at 96. Linux x64: mode at 24, size at 48;
        // Linux arm64: mode at 16, size at 48 (glibc bits/struct_stat.h).
        if ((BinaryPrimitives.ReadUInt16LittleEndian(status.AsSpan(UnixModeOffset())) & 0xf000) != 0x8000)
        {
            reason = "is not a regular file";
            return false;
        }

        length = BinaryPrimitives.ReadInt64LittleEndian(status.AsSpan(mac ? 96 : 48));
        return length >= 0;
    }

    static int UnixModeOffset() => (OperatingSystem.IsMacOS(), RuntimeInformation.ProcessArchitecture) switch
    {
        (true, _) => 4,
        (false, Architecture.X64) => 24,
        _ => 16
    };

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("libc", EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int Open(string path, int flags);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("libc", EntryPoint = "openat", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int OpenAt(SafeFileHandle directory, string path, int flags);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("libc", EntryPoint = "fstat")]
    private static partial int FStat(SafeFileHandle descriptor, [Out] byte[] status);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("libc", EntryPoint = "fstat$INODE64")]
    private static partial int FStatMacX64(SafeFileHandle descriptor, [Out] byte[] status);
}
