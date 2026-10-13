// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>Expands bound types into canonical stored schemas.</summary>
public static class ReleaseSchemaCanonical
{
    /// <summary>Resolves one complete event revision.</summary>
    /// <param name="application">The bound application.</param>
    /// <param name="eventType">The effective stored name.</param>
    /// <param name="properties">The revision properties.</param>
    /// <returns>The expanded schema.</returns>
    public static ReleaseSchema Resolve(SemanticApplication application, string eventType, ImmutableArray<SemanticProperty> properties) => new(eventType, Properties(application, properties, []));

    /// <summary>Serializes a schema without whitespace, in stable member order.</summary>
    /// <param name="schema">The resolved schema.</param>
    /// <returns>Canonical UTF-8 JSON.</returns>
    public static byte[] Serialize(ReleaseSchema schema) => JsonSerializer.SerializeToUtf8Bytes(Normalize(schema), ReleaseBaselineStore.Options);

    /// <summary>Computes the rsh1 hash over canonical schema bytes.</summary>
    /// <param name="schema">The resolved schema.</param>
    /// <returns>The versioned hash.</returns>
    public static string Hash(ReleaseSchema schema) => $"rsh1:{Convert.ToHexString(SHA256.HashData(Serialize(schema))).ToLowerInvariant()}";

    internal static ReleaseSchema Normalize(ReleaseSchema schema)
    {
        ModelNotesValidation.Text(schema.EventType);
        return schema with { Properties = NormalizeProperties(schema.Properties, 0) };
    }

    static ImmutableArray<ReleaseSchemaProperty> Properties(SemanticApplication application, ImmutableArray<SemanticProperty> properties, ImmutableArray<SemanticId> path) =>
        [.. properties.OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => new ReleaseSchemaProperty(property.Name, property.Type.IsOptional, property.Type.IsCollection, Type(application, property.Type, path)))];

    static ReleaseSchemaType Type(SemanticApplication application, SemanticTypeReference reference, ImmutableArray<SemanticId> path) => reference.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => new("primitive", reference.Primitive.ToString()),
        SemanticTypeReferenceKind.Concept => Concept(application.Concepts.Single(concept => concept.Id == reference.Target)),
        SemanticTypeReferenceKind.CompositeType => Composite(application, reference.Target, path),
        _ => throw new InvalidReleaseBaseline("Unresolved stored type.")
    };

    static ReleaseSchemaType Composite(SemanticApplication application, SemanticId target, ImmutableArray<SemanticId> path)
    {
        var ancestor = path.IndexOf(target);
        if (ancestor >= 0) return new("ref", Ref: path.Length - ancestor);
        return new("composite", Properties: Properties(application, application.Types.Single(type => type.Id == target).Properties, path.Add(target)));
    }

    static ReleaseSchemaType Concept(SemanticConcept concept) => concept.Values.IsEmpty
        ? new("primitive", concept.Primitive.ToString(), concept.Compliance)
        : new("enum", Members: [.. concept.Values.Select((name, index) => new ReleaseSchemaMember(name, concept.Numbers is { } numbers ? numbers[index] : index)).OrderBy(member => member.Number).ThenBy(member => member.Name, StringComparer.Ordinal)]);

    static ImmutableArray<ReleaseSchemaProperty> NormalizeProperties(ImmutableArray<ReleaseSchemaProperty> properties, int depth)
    {
        if (properties.IsDefault || properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length)
        {
            throw new InvalidReleaseBaseline("Stored properties must be present and unique.");
        }

        return [.. properties.OrderBy(property => property.Name, StringComparer.Ordinal).Select(property =>
        {
            ModelNotesValidation.Text(property.Name);
            return property with { Type = NormalizeType(property.Type, depth) };
        })];
    }

    static ReleaseSchemaType NormalizeType(ReleaseSchemaType type, int depth) => type.Kind switch
    {
        "primitive" when type.Ref is null && type.Members is null && type.Properties is null => NormalizePrimitive(type),
        "enum" when type.Ref is null && type.Primitive is null && type.Compliance is null && type.Properties is null && type.Members is { } members => type with { Members = NormalizeMembers(members) },
        "composite" when type.Ref is null && type.Primitive is null && type.Compliance is null && type.Members is null && type.Properties is { } properties => type with { Properties = NormalizeProperties(properties, depth + 1) },
        "ref" when type.Primitive is null && type.Compliance is null && type.Members is null && type.Properties is null && type.Ref is > 0 && type.Ref <= depth => type,
        _ => throw new InvalidReleaseBaseline("Invalid resolved stored type.")
    };

    static ReleaseSchemaType NormalizePrimitive(ReleaseSchemaType type)
    {
        if (!Enum.TryParse<SemanticPrimitiveType>(type.Primitive, out var primitive) || !Enum.IsDefined(primitive) || primitive == SemanticPrimitiveType.Unknown || primitive.ToString() != type.Primitive)
        {
            throw new InvalidReleaseBaseline("Invalid stored primitive.");
        }

        if (type.Compliance is { } compliance) ValidateCompliance(compliance);
        return type;
    }

    static void ValidateCompliance(SemanticConceptCompliance compliance)
    {
        if (compliance.Personal is null && compliance.Secret is null) throw new InvalidReleaseBaseline("Empty compliance marker.");
        if (compliance.Personal?.Special is { } special && !Parsing.ConceptComplianceParser.SpecialCategories.Contains(special, StringComparer.Ordinal))
        {
            throw new InvalidReleaseBaseline("Unknown personal classification.");
        }

        if (compliance.Secret?.Scope is { } scope)
        {
            if (compliance.Personal is not null) throw new InvalidReleaseBaseline("Personal markers cannot carry secret scope.");
            if (scope is not ("subject" or "namespace" or "global")) throw new InvalidReleaseBaseline("Unknown secret scope.");
        }
    }

    static ImmutableArray<ReleaseSchemaMember> NormalizeMembers(ImmutableArray<ReleaseSchemaMember> members)
    {
        if (members.IsDefaultOrEmpty || members.Select(member => member.Name).Distinct(StringComparer.Ordinal).Count() != members.Length || members.Select(member => member.Number).Distinct().Count() != members.Length)
        {
            throw new InvalidReleaseBaseline("Stored enum members must be nonempty and unique.");
        }

        foreach (var member in members) ModelNotesValidation.Text(member.Name);
        return [.. members.OrderBy(member => member.Number).ThenBy(member => member.Name, StringComparer.Ordinal)];
    }
}

/// <summary>The exception that is thrown when a release baseline has an invalid structural contract.</summary>
/// <param name="message">The structural failure.</param>
public sealed class InvalidReleaseBaseline(string message) : Exception(message);
