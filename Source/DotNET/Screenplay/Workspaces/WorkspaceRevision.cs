// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Represents the deterministic revision of an exact Screenplay authoring workspace.
/// </summary>
public readonly record struct WorkspaceRevision
{
    const string Prefix = "wsrev1:";
    readonly string? _value;

    WorkspaceRevision(string value) => _value = value;

    /// <summary>
    /// Gets a value indicating whether the revision is set.
    /// </summary>
    public bool IsSet => _value is not null;

    /// <summary>
    /// Parses a canonical workspace revision.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed revision.</returns>
    /// <exception cref="InvalidSemanticContract">The value is malformed.</exception>
    public static WorkspaceRevision Parse(string value) =>
        new(IdentityText.Parse(value, Prefix, nameof(WorkspaceRevision)));

    /// <summary>
    /// Tries to parse a canonical workspace revision.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="revision">The parsed revision when successful.</param>
    /// <returns><see langword="true"/> when the value is canonical; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? value, out WorkspaceRevision revision)
    {
        var success = IdentityText.TryParse(value, Prefix, out var canonical);
        revision = success ? new(canonical!) : default;
        return success;
    }

    /// <inheritdoc/>
    public override string ToString() => _value ?? string.Empty;

    internal static WorkspaceRevision Compute(ReadOnlySpan<byte> canonicalBytes) =>
        new(RevisionHash.Create(Prefix, "Cratis.Screenplay.WorkspaceRevision.v1", canonicalBytes));
}
