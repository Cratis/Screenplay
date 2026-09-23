// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp;

static class McpSemanticAddresses
{
    internal static object Describe(SemanticAddress address) => new { address.Kind, address.Parts };

    internal static SemanticAddress Read(JsonElement value)
    {
        McpJson.ValidateObject(value, ["kind", "parts"], ["kind", "parts"]);
        var kind = Named<SemanticKind>(value, "kind");
        var entries = value.GetProperty("parts");
        if (entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() is < 1 or > 128)
        {
            throw new McpFailure("An address must have 1–128 typed parts.", -32602);
        }

        var parts = entries.EnumerateArray().Select(part =>
        {
            McpJson.ValidateObject(part, ["kind", "key"], ["kind", "key"]);
            return SemanticAddressPart.Create(Named<SemanticAddressPartKind>(part, "kind"), McpJson.RequiredString(part, "key"));
        }).ToImmutableArray();
        var address = Build(kind, parts);
        if (!address.Parts.SequenceEqual(parts))
        {
            throw new McpFailure("Address parts do not form a canonical address of the selected kind.", -32602);
        }

        return address;
    }

    static SemanticAddress Build(SemanticKind kind, ImmutableArray<SemanticAddressPart> parts)
    {
        if (parts.IsEmpty || parts[0].Kind != SemanticAddressPartKind.Application)
        {
            throw new McpFailure("An address starts with its application identity.", -32602);
        }

        if (kind is SemanticKind.Property or SemanticKind.QueryArgument)
        {
            if (parts.Length < 3 || parts[^2].Kind != SemanticAddressPartKind.OwnerKind ||
                !Enum.TryParse<SemanticKind>(parts[^2].Key, out var ownerKind) || !Enum.IsDefined(ownerKind))
            {
                throw new McpFailure("A member address requires a valid owner kind.", -32602);
            }

            var owner = Build(ownerKind, parts[..^2]);
            return kind == SemanticKind.Property
                ? SemanticAddress.ForProperty(owner, parts[^1].Key)
                : SemanticAddress.ForQueryArgument(owner, parts[^1].Key);
        }

        var application = ApplicationIdentity.Parse(parts[0].Key);
        if (kind == SemanticKind.Application)
        {
            return SemanticAddress.ForApplication(application);
        }

        if (kind is SemanticKind.Concept or SemanticKind.CompositeType)
        {
            return kind == SemanticKind.Concept
                ? SemanticAddress.ForConcept(application, parts[^1].Key)
                : SemanticAddress.ForCompositeType(application, parts[^1].Key);
        }

        var module = Part(parts, SemanticAddressPartKind.Module);
        if (kind == SemanticKind.Module)
        {
            return SemanticAddress.ForModule(application, module);
        }

        var features = parts.Where(part => part.Kind == SemanticAddressPartKind.Feature).Select(part => part.Key).ToImmutableArray();
        if (kind == SemanticKind.Feature)
        {
            return SemanticAddress.ForFeature(application, module, features);
        }

        var slice = SemanticAddress.ForSlice(application, module, features, Part(parts, SemanticAddressPartKind.Slice));
        var name = parts[^1].Key;
        return kind switch
        {
            SemanticKind.Slice => slice,
            SemanticKind.Command => SemanticAddress.ForCommand(slice, name),
            SemanticKind.EventContract => SemanticAddress.ForEventContract(slice, name),
            SemanticKind.ReadModel => SemanticAddress.ForReadModel(slice, name),
            SemanticKind.Projection => SemanticAddress.ForProjection(slice, name),
            SemanticKind.Query => SemanticAddress.ForQuery(slice, name),
            SemanticKind.Specification => SemanticAddress.ForSpecification(slice, name),
            _ => throw new McpFailure($"Unsupported address kind '{kind}'.", -32602)
        };
    }

    static string Part(ImmutableArray<SemanticAddressPart> parts, SemanticAddressPartKind kind)
    {
        var matches = parts.Where(part => part.Kind == kind).ToArray();
        return matches.Length == 1 ? matches[0].Key : throw new McpFailure($"Address requires exactly one '{kind}' part.", -32602);
    }

    static T Named<T>(JsonElement value, string property)
        where T : struct, Enum
    {
        var text = McpJson.RequiredString(value, property);
        return Enum.TryParse<T>(text, out var result) && Enum.GetName(result) == text
            ? result
            : throw new McpFailure($"Unknown {typeof(T).Name} '{text}'.", -32602);
    }
}
