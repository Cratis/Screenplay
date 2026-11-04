// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Semantics;

/// <summary>Serializes the portable descriptor contract independently of model provenance.</summary>
public static class SemanticTypedContextSerializer
{
    static readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    static readonly string[] _typeKinds = [SemanticContextTypeKinds.Runtime, SemanticContextTypeKinds.Shape, SemanticContextTypeKinds.Model];

    static readonly string[] _sourceKinds =
    [
        SemanticContextSourceKinds.ContextContract, SemanticContextSourceKinds.Derived, SemanticContextSourceKinds.Command,
        SemanticContextSourceKinds.ReadModel, SemanticContextSourceKinds.CurrentEvent, SemanticContextSourceKinds.EventSourceId,
        SemanticContextSourceKinds.ModelProperty, SemanticContextSourceKinds.ConceptValue, SemanticContextSourceKinds.ValidatedProperty,
        SemanticContextSourceKinds.ValidatedArtifact, SemanticContextSourceKinds.AuthorizedOperation,
        SemanticContextSourceKinds.CommandIdentifier, SemanticContextSourceKinds.QueryKey, SemanticContextSourceKinds.Unavailable
    ];

    static readonly string[] _runtimeTokens =
    [
        SemanticContextRuntimeTokens.Text, SemanticContextRuntimeTokens.WholeNumber, SemanticContextRuntimeTokens.Boolean,
        SemanticContextRuntimeTokens.DateTime, SemanticContextRuntimeTokens.TenantId, SemanticContextRuntimeTokens.Identity,
        SemanticContextRuntimeTokens.CausedBy, SemanticContextRuntimeTokens.Causation
    ];

    /// <summary>Produces stable UTF-8 JSON bytes suitable for shared contract vectors.</summary>
    /// <param name="descriptors">The descriptors to serialize.</param>
    /// <remarks>
    /// Revision 1 carries no time-zone member. Revision 2 adds the <c>TimeZone</c> runtime token for <c>$context.timeZone</c>.
    /// Revision 3 (decision 0132) is written only when a declared application context member is read: it adds the
    /// <c>application-context</c> source kind and a top-level <c>contextMembers</c> registry, ordered by name.
    /// A descriptor with a handler result contract carries a <c>result</c> member (decision 0119).
    /// </remarks>
    public static byte[] Serialize(ImmutableArray<SemanticTypedContextDescriptor> descriptors)
    {
        var members = descriptors.SelectMany(descriptor => descriptor.Members).ToArray();
        var usesTimeZone = members.Any(member => member.Type.RuntimeToken == SemanticContextRuntimeTokens.TimeZone);
        var contextMembers = members
            .Where(member => member.Source.Kind == SemanticContextSourceKinds.ApplicationContext)
            .Select(member => member.Source.Path)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(name => new { name, runtimeToken = SemanticContextRuntimeTokens.TimeZone })
            .ToArray();
        string[] runtimeTokens = [.. _runtimeTokens, .. usesTimeZone ? [SemanticContextRuntimeTokens.TimeZone] : Array.Empty<string>()];
        var written = descriptors.Select(descriptor => WithResult(descriptor, Descriptor(descriptor)));
        object contract = contextMembers.Length > 0
            ? new
            {
                contractRevision = SemanticTypedContextDescriptor.ContextMembersContractRevision,
                typeKinds = _typeKinds,
                sourceKinds = (string[])[.. _sourceKinds, SemanticContextSourceKinds.ApplicationContext],
                runtimeTokens,
                contextMembers,
                descriptors = written
            }
            : new
            {
                contractRevision = usesTimeZone ? SemanticTypedContextDescriptor.TimeZoneContractRevision : SemanticTypedContextDescriptor.ContractRevision,
                typeKinds = _typeKinds,
                sourceKinds = _sourceKinds,
                runtimeTokens,
                descriptors = written
            };
        return JsonSerializer.SerializeToUtf8Bytes(contract, contract.GetType(), _options);
    }

    static object Descriptor(SemanticTypedContextDescriptor descriptor) => new
    {
        descriptor.RequirementId,
        role = descriptor.Role.ToString(),
        descriptor.ContextVersion,
        operationId = descriptor.OperationId?.ToString(),
        descriptor.IsWrapperReady,
        types = descriptor.Types.Select(type => new
        {
            id = type.Id.ToString(),
            type.Name,
            kind = type.Kind.ToString(),
            primitive = type.Primitive.ToString(),
            properties = type.Properties.Select(property => new { property.Name, id = property.Id.ToString(), type = Reference(property.Type) })
        }),
        members = descriptor.Members.Select(member => new
        {
            member.Name,
            member.IsNullable,
            member.IsDerived,
            type = new
            {
                member.Type.Kind,
                modelType = Reference(member.Type.ModelType),
                shape = member.Type.Shape?.ToString(),
                member.Type.RuntimeToken,
                properties = member.Type.Properties.Select(property => new { property.Name, id = property.Id.ToString(), type = Reference(property.Type) })
            },
            source = new
            {
                member.Source.Kind,
                semanticId = member.Source.SemanticId?.ToString(),
                member.Source.Path,
                member.Source.ConstantValue,
                eventRevision = member.Source.EventRevision?.Value
            }
        })
    };

    static object WithResult(SemanticTypedContextDescriptor descriptor, object legacy)
    {
        if (descriptor.Result is null) return legacy;
        var node = (JsonObject)JsonSerializer.SerializeToNode(legacy, _options)!;
        node["result"] = JsonSerializer.SerializeToNode(DescribeResult(descriptor.Result), _options);
        return node;
    }

    static object DescribeResult(SemanticHandlerResultDescriptor result) => new
    {
        result.ResultVersion,
        entries = result.Entries.Select(entry => new
        {
            eventContract = entry.EventContract.ToString(),
            entry.EventName,
            entry.EventRevision,
            payload = ContextType(entry.Payload),
            entry.DestinationKind,
            entry.DestinationProperty,
            destinationType = entry.DestinationType is null ? null : ContextType(entry.DestinationType)
        })
    };

    static object ContextType(SemanticContextType type) => new
    {
        type.Kind,
        modelType = Reference(type.ModelType),
        shape = type.Shape?.ToString(),
        type.RuntimeToken,
        properties = type.Properties.Select(property => new { property.Name, id = property.Id.ToString(), type = Reference(property.Type) })
    };

    static object? Reference(SemanticTypeReference? type) => type is null ? null : new
    {
        kind = type.Kind.ToString(),
        primitive = type.Primitive.ToString(),
        target = type.Kind == SemanticTypeReferenceKind.Primitive ? null : type.Target.ToString(),
        type.IsCollection,
        type.IsOptional
    };
}
