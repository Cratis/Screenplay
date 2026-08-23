// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines semantic artifact kinds used as identity domain separators.
/// </summary>
public enum SemanticKind
{
    /// <summary>
    /// An unknown kind. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// An application.
    /// </summary>
    Application = 0,

    /// <summary>
    /// A module.
    /// </summary>
    Module = 1,

    /// <summary>
    /// A feature.
    /// </summary>
    Feature = 2,

    /// <summary>
    /// A slice.
    /// </summary>
    Slice = 3,

    /// <summary>
    /// A concept.
    /// </summary>
    Concept = 4,

    /// <summary>
    /// A composite type.
    /// </summary>
    CompositeType = 5,

    /// <summary>
    /// A property.
    /// </summary>
    Property = 6,

    /// <summary>
    /// A command.
    /// </summary>
    Command = 7,

    /// <summary>
    /// An event contract.
    /// </summary>
    EventContract = 8,

    /// <summary>
    /// A read model.
    /// </summary>
    ReadModel = 9,

    /// <summary>
    /// A projection.
    /// </summary>
    Projection = 10,

    /// <summary>
    /// A query.
    /// </summary>
    Query = 11,

    /// <summary>
    /// A specification.
    /// </summary>
    Specification = 12
}

/// <summary>
/// Defines the role of a part in a <see cref="SemanticAddress"/>.
/// </summary>
public enum SemanticAddressPartKind
{
    /// <summary>
    /// An unknown part. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// An application name.
    /// </summary>
    Application = 0,

    /// <summary>
    /// A module name.
    /// </summary>
    Module = 1,

    /// <summary>
    /// A feature name.
    /// </summary>
    Feature = 2,

    /// <summary>
    /// A slice name.
    /// </summary>
    Slice = 3,

    /// <summary>
    /// A declaration name.
    /// </summary>
    Declaration = 4,

    /// <summary>
    /// A member name.
    /// </summary>
    Member = 5,

    /// <summary>
    /// A stable explicit discriminator.
    /// </summary>
    Discriminator = 6
}

/// <summary>
/// Represents one typed, Unicode-normalized component of a semantic address.
/// </summary>
/// <param name="Kind">The role of the component.</param>
/// <param name="Key">The Unicode NFC key.</param>
public readonly record struct SemanticAddressPart(SemanticAddressPartKind Kind, string Key)
{
    /// <summary>
    /// Creates a validated address part.
    /// </summary>
    /// <param name="kind">The role of the component.</param>
    /// <param name="key">The key to normalize to Unicode NFC.</param>
    /// <returns>The validated part.</returns>
    /// <exception cref="InvalidSemanticContract">The kind or key is invalid.</exception>
    public static SemanticAddressPart Create(SemanticAddressPartKind kind, string key)
    {
        if (!Enum.IsDefined(kind) || kind == SemanticAddressPartKind.Unknown)
        {
            throw new InvalidSemanticContract($"Semantic address part kind '{(int)kind}' is unknown.");
        }

        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidSemanticContract("A semantic address part key cannot be empty.");
        }

        return new(kind, key.Normalize(NormalizationForm.FormC));
    }
}

/// <summary>
/// Represents the structured, location-independent bootstrap address of a semantic artifact.
/// </summary>
public sealed class SemanticAddress : IEquatable<SemanticAddress>
{
    SemanticAddress(SemanticKind kind, ImmutableArray<SemanticAddressPart> parts)
    {
        Kind = kind;
        Parts = parts;
    }

    /// <summary>
    /// Gets the artifact kind.
    /// </summary>
    public SemanticKind Kind { get; }

    /// <summary>
    /// Gets the typed address parts in parent-to-child order.
    /// </summary>
    public ImmutableArray<SemanticAddressPart> Parts { get; }

    /// <summary>
    /// Creates a validated semantic address.
    /// </summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="parts">The typed address parts.</param>
    /// <returns>The validated address.</returns>
    /// <exception cref="InvalidSemanticContract">The kind, parts, or keys are invalid.</exception>
    public static SemanticAddress Create(SemanticKind kind, ImmutableArray<SemanticAddressPart> parts)
    {
        if (!Enum.IsDefined(kind) || kind == SemanticKind.Unknown)
        {
            throw new InvalidSemanticContract($"Semantic kind '{(int)kind}' is unknown.");
        }

        if (parts.IsDefault || parts.IsEmpty)
        {
            throw new InvalidSemanticContract("A semantic address must contain at least one part.");
        }

        var normalized = ImmutableArray.CreateBuilder<SemanticAddressPart>(parts.Length);
        foreach (var part in parts)
        {
            normalized.Add(SemanticAddressPart.Create(part.Kind, part.Key));
        }

        return new(kind, normalized.MoveToImmutable());
    }

    /// <inheritdoc/>
    public bool Equals(SemanticAddress? other) =>
        other is not null && Kind == other.Kind && Parts.SequenceEqual(other.Parts);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SemanticAddress other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Kind);
        foreach (var part in Parts)
        {
            hash.Add(part.Kind);
            hash.Add(part.Key, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
