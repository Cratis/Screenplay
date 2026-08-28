// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Represents one normalized portable relative path to a Screenplay source document.
/// </summary>
public sealed record PortablePlayPath
{
    const int MaxPathLength = 4096;
    const int MaxSegmentLength = 255;
    static readonly char[] _invalidCharacters = ['<', '>', ':', '"', '|', '?', '*'];
    static readonly UTF8Encoding _strictUtf8 = new(false, true);
    static readonly HashSet<string> _reservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        "COM¹", "COM²", "COM³", "LPT¹", "LPT²", "LPT³"
    };

    PortablePlayPath(string value) => Value = value;

    /// <summary>
    /// Gets the comparer used to detect portable path aliases independently of authored casing.
    /// </summary>
    public static IEqualityComparer<PortablePlayPath> CollisionComparer { get; } = new PortablePathCollisionComparer();

    /// <summary>
    /// Gets the normalized path using <c>/</c> separators and Unicode NFC.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Parses and validates a portable Screenplay source path.
    /// </summary>
    /// <param name="value">The path to parse.</param>
    /// <returns>The normalized portable path.</returns>
    /// <exception cref="InvalidPortablePlayPath">The path is not a portable relative <c>.play</c> path.</exception>
    public static PortablePlayPath Parse(string value) => new(Normalize(value));

    /// <summary>
    /// Tries to parse and validate a portable Screenplay source path.
    /// </summary>
    /// <param name="value">The path to parse.</param>
    /// <param name="path">The normalized path when successful.</param>
    /// <returns><see langword="true"/> when the path is valid; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? value, [NotNullWhen(true)] out PortablePlayPath? path)
    {
        try
        {
            path = value is null ? null : Parse(value);
            return path is not null;
        }
        catch (InvalidPortablePlayPath)
        {
            path = null;
            return false;
        }
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    static string Normalize(string value)
    {
        if (value is null)
        {
            throw new InvalidPortablePlayPath(string.Empty, "The path cannot be null.");
        }

        try
        {
            SemanticDocumentText.RequireWellFormedUnicode(value, "workspace document path");
        }
        catch (InvalidSemanticContract exception)
        {
            throw new InvalidPortablePlayPath(value, exception.Message);
        }

        var normalized = value.Normalize(NormalizationForm.FormC).Replace('\\', '/');
        if (normalized.Length == 0 || normalized.Length > MaxPathLength ||
            _strictUtf8.GetByteCount(normalized) > MaxPathLength ||
            normalized[0] == '/' || IsDrivePath(normalized) || normalized.StartsWith("//", StringComparison.Ordinal))
        {
            throw new InvalidPortablePlayPath(value, "The path must be nonempty, relative, and within the portable length limit.");
        }

        foreach (var segment in normalized.Split('/'))
        {
            ValidateSegment(value, segment);
        }

        if (!normalized.EndsWith(".play", StringComparison.Ordinal))
        {
            throw new InvalidPortablePlayPath(value, "A Screenplay workspace document path must end with the exact '.play' extension.");
        }

        return normalized;
    }

    static void ValidateSegment(string path, string segment)
    {
        if (segment.Length == 0 || segment.Length > MaxSegmentLength ||
            _strictUtf8.GetByteCount(segment) > MaxSegmentLength ||
            string.Equals(segment, ".", StringComparison.Ordinal) ||
            string.Equals(segment, "..", StringComparison.Ordinal) ||
            segment[^1] == '.' ||
            segment[^1] == ' ' ||
            segment.Any(character => char.IsControl(character) || _invalidCharacters.Contains(character)))
        {
            throw new InvalidPortablePlayPath(path, $"Path segment '{segment}' is not portable.");
        }

        var deviceName = segment.Split('.', 2)[0];
        if (_reservedNames.Contains(deviceName))
        {
            throw new InvalidPortablePlayPath(path, $"Path segment '{segment}' uses a reserved device name.");
        }
    }

    static bool IsDrivePath(string value) => value.Length >= 2 && char.IsAsciiLetter(value[0]) && value[1] == ':';

    sealed class PortablePathCollisionComparer : IEqualityComparer<PortablePlayPath>
    {
        public bool Equals(PortablePlayPath? x, PortablePlayPath? y) => ReferenceEquals(x, y) ||
            (x is not null && y is not null && string.Equals(x.Value, y.Value, StringComparison.OrdinalIgnoreCase));

        public int GetHashCode(PortablePlayPath obj) => StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Value);
    }
}

/// <summary>
/// The exception that is thrown when a workspace document path is not portable.
/// </summary>
/// <param name="path">The rejected path.</param>
/// <param name="reason">The reason the path was rejected.</param>
public sealed class InvalidPortablePlayPath(string path, string reason)
    : Exception($"The workspace document path '{path}' is invalid. {reason}");
