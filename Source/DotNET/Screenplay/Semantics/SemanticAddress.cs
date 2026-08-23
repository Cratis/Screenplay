// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
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
    /// An application identity.
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
    /// A stable explicit discriminator used outside semantic addresses.
    /// </summary>
    Discriminator = 6,

    /// <summary>
    /// The semantic kind of a property's owner.
    /// </summary>
    OwnerKind = 7
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
/// Represents a legal, structured, location-independent address of a semantic artifact.
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
    /// Gets the declaration or member name represented by the address.
    /// </summary>
    public string Name => Parts[^1].Key;

    /// <summary>
    /// Gets the application identity represented by the address.
    /// </summary>
    public ApplicationIdentity Application => ApplicationIdentity.Parse(Parts[0].Key);

    /// <summary>
    /// Gets the property owner's semantic kind, or <see cref="SemanticKind.Unknown"/> for a non-property address.
    /// </summary>
    public SemanticKind OwnerKind => Kind == SemanticKind.Property ? ParseOwnerKind(Parts[^2]) : SemanticKind.Unknown;

    /// <summary>
    /// Creates an application address.
    /// </summary>
    /// <param name="application">The application identity.</param>
    /// <returns>The application address.</returns>
    public static SemanticAddress ForApplication(ApplicationIdentity application) =>
        Build(SemanticKind.Application, [ApplicationPart(application)]);

    /// <summary>
    /// Creates a module address.
    /// </summary>
    /// <param name="application">The application identity.</param>
    /// <param name="module">The module name.</param>
    /// <returns>The module address.</returns>
    public static SemanticAddress ForModule(ApplicationIdentity application, string module) =>
        Build(SemanticKind.Module, [ApplicationPart(application), Part(SemanticAddressPartKind.Module, module)]);

    /// <summary>
    /// Creates a feature address.
    /// </summary>
    /// <param name="application">The application identity.</param>
    /// <param name="module">The module name.</param>
    /// <param name="feature">The feature name.</param>
    /// <returns>The feature address.</returns>
    public static SemanticAddress ForFeature(ApplicationIdentity application, string module, string feature) =>
        ForFeature(application, module, [feature]);

    /// <summary>
    /// Creates a nested feature address.
    /// </summary>
    /// <param name="application">The application identity.</param>
    /// <param name="module">The module name.</param>
    /// <param name="featurePath">The non-empty feature path.</param>
    /// <returns>The feature address.</returns>
    public static SemanticAddress ForFeature(ApplicationIdentity application, string module, ImmutableArray<string> featurePath) =>
        BuildHierarchy(SemanticKind.Feature, application, module, featurePath, null);

    /// <summary>
    /// Creates a slice address.
    /// </summary>
    /// <param name="application">The application identity.</param>
    /// <param name="module">The module name.</param>
    /// <param name="feature">The feature name.</param>
    /// <param name="slice">The slice name.</param>
    /// <returns>The slice address.</returns>
    public static SemanticAddress ForSlice(ApplicationIdentity application, string module, string feature, string slice) =>
        ForSlice(application, module, [feature], slice);

    /// <summary>
    /// Creates a slice address below a nested feature path.
    /// </summary>
    /// <param name="application">The application identity.</param>
    /// <param name="module">The module name.</param>
    /// <param name="featurePath">The non-empty feature path.</param>
    /// <param name="slice">The slice name.</param>
    /// <returns>The slice address.</returns>
    public static SemanticAddress ForSlice(ApplicationIdentity application, string module, ImmutableArray<string> featurePath, string slice) =>
        BuildHierarchy(SemanticKind.Slice, application, module, featurePath, slice);

    /// <summary>
    /// Creates an application concept address.
    /// </summary>
    /// <param name="application">The application identity.</param>
    /// <param name="name">The concept name.</param>
    /// <returns>The concept address.</returns>
    public static SemanticAddress ForConcept(ApplicationIdentity application, string name) =>
        BuildApplicationDeclaration(SemanticKind.Concept, application, name);

    /// <summary>
    /// Creates an application composite-type address.
    /// </summary>
    /// <param name="application">The application identity.</param>
    /// <param name="name">The type name.</param>
    /// <returns>The composite-type address.</returns>
    public static SemanticAddress ForCompositeType(ApplicationIdentity application, string name) =>
        BuildApplicationDeclaration(SemanticKind.CompositeType, application, name);

    /// <summary>
    /// Creates a command address in a slice.
    /// </summary>
    /// <param name="slice">The owning slice address.</param>
    /// <param name="name">The command name.</param>
    /// <returns>The command address.</returns>
    public static SemanticAddress ForCommand(SemanticAddress slice, string name) => BuildSliceDeclaration(SemanticKind.Command, slice, name);

    /// <summary>
    /// Creates an event-contract address in a slice.
    /// </summary>
    /// <param name="slice">The owning slice address.</param>
    /// <param name="name">The exact event name.</param>
    /// <returns>The event-contract address.</returns>
    public static SemanticAddress ForEventContract(SemanticAddress slice, string name) => BuildSliceDeclaration(SemanticKind.EventContract, slice, name);

    /// <summary>
    /// Creates a read-model address in a slice.
    /// </summary>
    /// <param name="slice">The owning slice address.</param>
    /// <param name="name">The read-model name.</param>
    /// <returns>The read-model address.</returns>
    public static SemanticAddress ForReadModel(SemanticAddress slice, string name) => BuildSliceDeclaration(SemanticKind.ReadModel, slice, name);

    /// <summary>
    /// Creates a projection address in a slice.
    /// </summary>
    /// <param name="slice">The owning slice address.</param>
    /// <param name="name">The projection name.</param>
    /// <returns>The projection address.</returns>
    public static SemanticAddress ForProjection(SemanticAddress slice, string name) => BuildSliceDeclaration(SemanticKind.Projection, slice, name);

    /// <summary>
    /// Creates a query address in a slice.
    /// </summary>
    /// <param name="slice">The owning slice address.</param>
    /// <param name="name">The query name.</param>
    /// <returns>The query address.</returns>
    public static SemanticAddress ForQuery(SemanticAddress slice, string name) => BuildSliceDeclaration(SemanticKind.Query, slice, name);

    /// <summary>
    /// Creates a specification address in a slice.
    /// </summary>
    /// <param name="slice">The owning slice address.</param>
    /// <param name="name">The specification name.</param>
    /// <returns>The specification address.</returns>
    public static SemanticAddress ForSpecification(SemanticAddress slice, string name) => BuildSliceDeclaration(SemanticKind.Specification, slice, name);

    /// <summary>
    /// Creates a property address below a legal property owner.
    /// </summary>
    /// <param name="owner">A composite-type, command, event-contract, or read-model address.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property address.</returns>
    public static SemanticAddress ForProperty(SemanticAddress owner, string name)
    {
        if (owner is null || owner.Kind is not (SemanticKind.CompositeType or SemanticKind.Command or SemanticKind.EventContract or SemanticKind.ReadModel))
        {
            throw new InvalidSemanticContract("A property address requires a composite-type, command, event-contract, or read-model owner.");
        }

        return Build(SemanticKind.Property, [.. owner.Parts, OwnerKindPart(owner.Kind), Part(SemanticAddressPartKind.Member, name)]);
    }

    /// <inheritdoc/>
    public bool Equals(SemanticAddress? other) => other is not null && Kind == other.Kind && Parts.SequenceEqual(other.Parts);

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

    internal static SemanticAddress FromCanonical(SemanticKind kind, ImmutableArray<SemanticAddressPart> parts) => Build(kind, parts);

    static SemanticAddress BuildApplicationDeclaration(SemanticKind kind, ApplicationIdentity application, string name) =>
        Build(kind, [ApplicationPart(application), Part(SemanticAddressPartKind.Declaration, name)]);

    static SemanticAddress BuildSliceDeclaration(SemanticKind kind, SemanticAddress slice, string name)
    {
        if (slice is null || slice.Kind != SemanticKind.Slice)
        {
            throw new InvalidSemanticContract($"A {kind} address requires a slice address.");
        }

        return Build(kind, [.. slice.Parts, Part(SemanticAddressPartKind.Declaration, name)]);
    }

    static SemanticAddress BuildHierarchy(
        SemanticKind kind,
        ApplicationIdentity application,
        string module,
        ImmutableArray<string> featurePath,
        string? slice)
    {
        if (featurePath.IsDefaultOrEmpty)
        {
            throw new InvalidSemanticContract("A feature path cannot be default or empty.");
        }

        var parts = ImmutableArray.CreateBuilder<SemanticAddressPart>();
        parts.Add(ApplicationPart(application));
        parts.Add(Part(SemanticAddressPartKind.Module, module));
        parts.AddRange(featurePath.Select(_ => Part(SemanticAddressPartKind.Feature, _)));
        if (slice is not null)
        {
            parts.Add(Part(SemanticAddressPartKind.Slice, slice));
        }

        return Build(kind, parts.ToImmutable());
    }

    static SemanticAddress Build(SemanticKind kind, ImmutableArray<SemanticAddressPart> parts)
    {
        if (!Enum.IsDefined(kind) || kind == SemanticKind.Unknown || parts.IsDefaultOrEmpty)
        {
            throw new InvalidSemanticContract("A semantic address kind and parts must be known and non-empty.");
        }

        var normalized = parts.Select(_ => SemanticAddressPart.Create(_.Kind, _.Key)).ToImmutableArray();
        ValidateShape(kind, normalized);
        return new(kind, normalized);
    }

    static void ValidateShape(SemanticKind kind, ImmutableArray<SemanticAddressPart> parts)
    {
        if (!ApplicationIdentity.TryParse(parts[0].Key, out _) || parts[0].Kind != SemanticAddressPartKind.Application)
        {
            throw new InvalidSemanticContract("A semantic address must start with a canonical application identity.");
        }

        var legal = kind switch
        {
            SemanticKind.Application => Matches(parts, SemanticAddressPartKind.Application),
            SemanticKind.Module => Matches(parts, SemanticAddressPartKind.Application, SemanticAddressPartKind.Module),
            SemanticKind.Feature => IsFeature(parts),
            SemanticKind.Slice => IsSlice(parts),
            SemanticKind.Concept or SemanticKind.CompositeType => Matches(parts, SemanticAddressPartKind.Application, SemanticAddressPartKind.Declaration),
            SemanticKind.Command or SemanticKind.EventContract or SemanticKind.ReadModel or SemanticKind.Projection or SemanticKind.Query or SemanticKind.Specification => IsSliceDeclaration(parts),
            SemanticKind.Property => IsProperty(parts),
            _ => false
        };
        if (!legal)
        {
            throw new InvalidSemanticContract($"Semantic kind '{kind}' cannot use the supplied address-part shape.");
        }
    }

    static bool IsFeature(ImmutableArray<SemanticAddressPart> parts) =>
        parts.Length >= 3 && parts[1].Kind == SemanticAddressPartKind.Module && parts[2..].All(_ => _.Kind == SemanticAddressPartKind.Feature);

    static bool IsSlice(ImmutableArray<SemanticAddressPart> parts) =>
        parts.Length >= 4 && parts[1].Kind == SemanticAddressPartKind.Module &&
        parts[^1].Kind == SemanticAddressPartKind.Slice && parts[2..^1].All(_ => _.Kind == SemanticAddressPartKind.Feature);

    static bool IsSliceDeclaration(ImmutableArray<SemanticAddressPart> parts) =>
        parts.Length >= 5 && parts[^1].Kind == SemanticAddressPartKind.Declaration && IsSlice(parts[..^1]);

    static bool IsProperty(ImmutableArray<SemanticAddressPart> parts)
    {
        if (parts.Length < 4 || parts[^1].Kind != SemanticAddressPartKind.Member || !TryParseOwnerKind(parts[^2], out var ownerKind))
        {
            return false;
        }

        var ownerParts = parts[..^2];
        return ownerKind switch
        {
            SemanticKind.CompositeType => Matches(ownerParts, SemanticAddressPartKind.Application, SemanticAddressPartKind.Declaration),
            SemanticKind.Command or SemanticKind.EventContract or SemanticKind.ReadModel => IsSliceDeclaration(ownerParts),
            _ => false
        };
    }

    static bool Matches(ImmutableArray<SemanticAddressPart> parts, params SemanticAddressPartKind[] kinds) =>
        parts.Length == kinds.Length && parts.Select(_ => _.Kind).SequenceEqual(kinds);

    static SemanticAddressPart ApplicationPart(ApplicationIdentity application)
    {
        if (!application.IsSet)
        {
            throw new InvalidSemanticContract("A semantic address requires an application identity.");
        }

        return Part(SemanticAddressPartKind.Application, application.ToString());
    }

    static SemanticAddressPart OwnerKindPart(SemanticKind ownerKind) =>
        Part(SemanticAddressPartKind.OwnerKind, ((int)ownerKind).ToString(CultureInfo.InvariantCulture));

    static SemanticKind ParseOwnerKind(SemanticAddressPart part)
    {
        if (!TryParseOwnerKind(part, out var ownerKind))
        {
            throw new InvalidSemanticContract("A property address contains an invalid owner kind.");
        }

        return ownerKind;
    }

    static bool TryParseOwnerKind(SemanticAddressPart part, out SemanticKind ownerKind)
    {
        ownerKind = SemanticKind.Unknown;
        if (part.Kind != SemanticAddressPartKind.OwnerKind ||
            !int.TryParse(part.Key, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ||
            value.ToString(CultureInfo.InvariantCulture) != part.Key ||
            !Enum.IsDefined(typeof(SemanticKind), value))
        {
            return false;
        }

        ownerKind = (SemanticKind)value;
        return ownerKind is SemanticKind.CompositeType or SemanticKind.Command or SemanticKind.EventContract or SemanticKind.ReadModel;
    }

    static SemanticAddressPart Part(SemanticAddressPartKind kind, string key) => SemanticAddressPart.Create(kind, key);
}
