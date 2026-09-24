// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>Reads referenced implementation files without following links or escaping the supplied root.</summary>
public static partial class AttachmentFiles
{
    /// <summary>Maximum bytes in one attachment, matching the MCP source-file limit.</summary>
    public static readonly int MaximumFileBytes = 2 * 1024 * 1024;

    /// <summary>Maximum bytes across attachments, matching the MCP source-set limit.</summary>
    public static readonly int MaximumBytes = 8 * 1024 * 1024;

    /// <summary>
    /// Loads implementation references from exact semantic source documents. Declaration-only file references are not read.
    /// </summary>
    /// <param name="root">The trusted physical model root.</param>
    /// <param name="documents">The source documents to inspect.</param>
    /// <returns>Contents and warnings; a refused reference is omitted from contents.</returns>
    public static AttachmentFileResult Load(string root, ImmutableArray<SemanticSourceDocument> documents) => Load(root, documents, IsRegularFile);

    /// <summary>Normalizes an authored path for host keys and binder lookup, without accessing the file system.</summary>
    /// <param name="path">The authored file path.</param>
    /// <param name="normalized">The normalized repository-relative key, if valid.</param>
    /// <param name="reason">The reason for refusal, if invalid.</param>
    /// <returns>Whether the path stays within the root.</returns>
    public static bool TryNormalize(string path, out string normalized, out string reason)
    {
        normalized = string.Empty;
        reason = string.Empty;
        var portable = path.Replace('\\', '/');
        if (portable.StartsWith('/') || portable.StartsWith("~/", StringComparison.Ordinal) ||
            (portable.Length >= 2 && char.IsAsciiLetter(portable[0]) && portable[1] == ':'))
        {
            reason = "absolute or drive-qualified";
            return false;
        }

        var segments = new List<string>();
        foreach (var segment in portable.Split('/'))
        {
            if (segment.Length == 0 || string.Equals(segment, ".", StringComparison.Ordinal))
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count == 0)
                {
                    reason = "outside the model root";
                    return false;
                }

                segments.RemoveAt(segments.Count - 1);
            }
            else if (segment.Contains(':') || segment.Any(char.IsControl) || IsReservedDeviceName(segment))
            {
                reason = "not a portable path";
                return false;
            }
            else
            {
                segments.Add(segment);
            }
        }

        if (segments.Count == 0)
        {
            reason = "not a file path";
            return false;
        }

        normalized = string.Join('/', segments);
        return true;
    }

    // A null result means the platform could not establish the file type; never open the attachment in that case.
    internal static AttachmentFileResult Load(string root, ImmutableArray<SemanticSourceDocument> documents, Func<string, bool?> verifyRegularFile)
    {
        var contents = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var fullRoot = Path.GetFullPath(root);
        var total = 0L;
        foreach (var document in documents)
        {
            var parsed = new ScreenplayCompiler().Parse(document.Text, document.DisplayPath);
            if (parsed.Value is null)
            {
                continue;
            }

            var references = new ImplementationReferences();
            references.VisitApplication(parsed.Value);
            foreach (var reference in references.Files)
            {
                if (!TryNormalize(reference.Path, out var key, out var reason))
                {
                    diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentPathRefused, $"File attachment '{reference.Path}' is {reason}.", reference.Location));
                    continue;
                }

                if (contents.ContainsKey(key))
                {
                    continue;
                }

                try
                {
                    if (File.GetAttributes(fullRoot).HasFlag(FileAttributes.ReparsePoint))
                    {
                        diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentLinkRefused, $"File attachment '{reference.Path}' has a symbolic-link or reparse-point model root.", reference.Location));
                        continue;
                    }
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentUnreadable, $"File attachment '{reference.Path}' cannot be inspected.", reference.Location));
                    continue;
                }

                var path = fullRoot;
                var refused = false;
                foreach (var segment in key.Split('/'))
                {
                    path = Path.Combine(path, segment);
                    try
                    {
                        var attributes = File.GetAttributes(path);
                        if (attributes.HasFlag(FileAttributes.ReparsePoint))
                        {
                            diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentLinkRefused, $"File attachment '{reference.Path}' crosses a symbolic link or reparse point.", reference.Location));
                            refused = true;
                            break;
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentMissing, $"File attachment '{reference.Path}' is missing.", reference.Location));
                        refused = true;
                        break;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentMissing, $"File attachment '{reference.Path}' is missing.", reference.Location));
                        refused = true;
                        break;
                    }
                    catch (IOException)
                    {
                        diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentUnreadable, $"File attachment '{reference.Path}' cannot be inspected.", reference.Location));
                        refused = true;
                        break;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentUnreadable, $"File attachment '{reference.Path}' cannot be inspected.", reference.Location));
                        refused = true;
                        break;
                    }
                }

                if (refused)
                {
                    continue;
                }

                path = Path.GetFullPath(path);
                if (!path.StartsWith(fullRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentPathRefused, $"File attachment '{reference.Path}' is outside the model root.", reference.Location));
                    continue;
                }

                try
                {
                    var attributes = File.GetAttributes(path);
                    if (attributes.HasFlag(FileAttributes.Directory))
                    {
                        diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentUnreadable, $"File attachment '{reference.Path}' is a directory.", reference.Location));
                        continue;
                    }

                    if (attributes.HasFlag(FileAttributes.Device) || attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentUnreadable, $"File attachment '{reference.Path}' is not a regular file.", reference.Location));
                        continue;
                    }

                    var isRegular = verifyRegularFile(path);
                    if (isRegular is null)
                    {
                        diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentUnreadable, $"File attachment '{reference.Path}' cannot verify the file type on this platform.", reference.Location));
                        continue;
                    }

                    if (!isRegular.Value)
                    {
                        diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentUnreadable, $"File attachment '{reference.Path}' is not a regular file.", reference.Location));
                        continue;
                    }

                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    if (!stream.CanSeek)
                    {
                        diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentUnreadable, $"File attachment '{reference.Path}' is not seekable.", reference.Location));
                        continue;
                    }

                    if (stream.Length > MaximumFileBytes || stream.Length > MaximumBytes - total)
                    {
                        diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentTooLarge, $"File attachment '{reference.Path}' exceeds the per-file or total byte limit.", reference.Location));
                        continue;
                    }

                    var bytes = new byte[checked((int)stream.Length)];
                    stream.ReadExactly(bytes);
                    if (stream.ReadByte() != -1 || HasLink(fullRoot, key))
                    {
                        diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentUnreadable, $"File attachment '{reference.Path}' changed while being read.", reference.Location));
                        continue;
                    }

                    var text = new UTF8Encoding(false, true).GetString(bytes);
                    contents.Add(key, text);
                    total += bytes.Length;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DecoderFallbackException or NotSupportedException)
                {
                    diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.AttachmentUnreadable, $"File attachment '{reference.Path}' cannot be read as UTF-8 text.", reference.Location));
                }
            }
        }

        return new() { Contents = contents.ToImmutable(), Diagnostics = diagnostics.ToImmutable() };
    }

    static bool IsReservedDeviceName(string segment)
    {
        var stem = segment.Split('.')[0].TrimEnd(' ');
        return stem.Equals("CON", StringComparison.OrdinalIgnoreCase) || stem.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
            stem.Equals("AUX", StringComparison.OrdinalIgnoreCase) || stem.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
            (stem.Length == 4 && stem[3] is >= '1' and <= '9' &&
             (stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)));
    }

    // lstat checks the file type without opening it: opening a FIFO for reading can block indefinitely.
    // Darwin's 64-bit-inode stat has mode_t (16 bits) at offset 4; on x64 its symbol is lstat$INODE64.
    // Linux x64/arm64 have a 32-bit mode_t at offsets 24/16; the low 16 bits contain S_IFMT.
    // Unsupported ABIs and unavailable libc symbols are refused rather than guessed.
    static bool? IsRegularFile(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return true;
        }

        var architecture = RuntimeInformation.ProcessArchitecture;
        var modeOffset = -1;
        if (OperatingSystem.IsMacOS() && architecture is Architecture.X64 or Architecture.Arm64)
        {
            modeOffset = 4;
        }
        else if (OperatingSystem.IsLinux())
        {
            modeOffset = architecture switch
            {
                Architecture.X64 => 24,
                Architecture.Arm64 => 16,
                _ => -1
            };
        }
        if (modeOffset < 0)
        {
            return null;
        }

        // Large enough for every supported struct stat ABI; native lstat writes the whole structure.
        var status = new byte[512];
        try
        {
            var result = OperatingSystem.IsMacOS() && architecture == Architecture.X64 ? LStatMacX64(path, status) : LStat(path, status);
            return result == 0 ? (BinaryPrimitives.ReadUInt16LittleEndian(status.AsSpan(modeOffset)) & 0xf000) == 0x8000 : null;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            return null;
        }
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("libc", EntryPoint = "lstat", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int LStat(string path, [Out] byte[] status);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("libc", EntryPoint = "lstat$INODE64", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int LStatMacX64(string path, [Out] byte[] status);

    static bool HasLink(string root, string key)
    {
        if (File.GetAttributes(root).HasFlag(FileAttributes.ReparsePoint))
        {
            return true;
        }

        var current = root;
        foreach (var segment in key.Split('/'))
        {
            current = Path.Combine(current, segment);
            if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            {
                return true;
            }
        }

        return false;
    }

    sealed class ImplementationReferences : ScreenplaySyntaxWalker
    {
        internal List<FileReferenceSyntax> Files { get; } = [];

        public override void VisitNode(SyntaxNode node)
        {
            var file = node switch
            {
                HandlerSyntax value => value.File,
                PerformerSyntax value => value.File,
                ValidationRuleSyntax value => value.File,
                ReducerRuleSyntax value => value.File,
                ReactionTriggerSyntax value => value.File,
                FileConstraintSyntax value => value.File,
                PolicySyntax value => value.File,
                _ => null
            };
            if (file is not null)
            {
                Files.Add(file);
            }
        }
    }
}
