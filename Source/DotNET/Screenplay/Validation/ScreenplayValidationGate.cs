// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Validation;

/// <summary>
/// Reads the authored screenplay.json without writing any model or tool state.
/// </summary>
public static class ScreenplayValidationGate
{
    /// <summary>
    /// Reads a strict v1 gate. A missing file is distinct from a refused file.
    /// </summary>
    /// <param name="modelRoot">The owning model root.</param>
    /// <returns>The declaration, absence, or a located typed refusal.</returns>
    public static ValidationGateReadResult Read(string modelRoot)
    {
        var root = Path.GetFullPath(modelRoot);
        var path = Path.Combine(root, "screenplay.json");
        try
        {
            return ReadFile(root, path);
        }
        catch (JsonException exception)
        {
            return new(root, path, null, new(ValidationGateFailureKind.Invalid, path, (exception.LineNumber ?? 0) + 1, (exception.BytePositionInLine ?? 0) + 1, exception.Message));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Unreadable(root, path, exception.Message);
        }
    }

    static ValidationGateReadResult ReadFile(string root, string path)
    {
        FileAttributes attributes;
        try
        {
            if (new FileInfo(path).LinkTarget is not null) return Unreadable(root, path, "Expected a regular file, not a symlink or reparse point.");
            attributes = File.GetAttributes(path);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return new(root, path, null, null);
        }

        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != default) return Unreadable(root, path, "Expected a regular file, not a directory, symlink or reparse point.");
        return ValidationGateJson.Read(root, path, WithoutByteOrderMark(File.ReadAllBytes(path)));
    }

    static byte[] WithoutByteOrderMark(byte[] bytes) => bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble) ? bytes[Encoding.UTF8.Preamble.Length..] : bytes;

    static ValidationGateReadResult Unreadable(string root, string path, string reason) => new(root, path, null, new(ValidationGateFailureKind.Unreadable, path, 1, 1, reason));
}
