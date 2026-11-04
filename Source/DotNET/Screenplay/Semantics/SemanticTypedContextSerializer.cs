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

    /// <summary>Produces stable UTF-8 JSON bytes suitable for shared contract vectors.</summary>
    /// <param name="descriptors">The descriptors to serialize.</param>
    public static byte[] Serialize(ImmutableArray<SemanticTypedContextDescriptor> descriptors)
    {
        var usesTimeZone = descriptors.Any(descriptor => descriptor.Members.Any(member => member.Type.RuntimeToken == SemanticContextRuntimeTokens.TimeZone));
        string[] runtimeTokens =
        [
            SemanticContextRuntimeTokens.Text, SemanticContextRuntimeTokens.WholeNumber, SemanticContextRuntimeTokens.Boolean,
            SemanticContextRuntimeTokens.DateTime, SemanticContextRuntimeTokens.TenantId, SemanticContextRuntimeTokens.Identity,
            SemanticContextRuntimeTokens.CausedBy, SemanticContextRuntimeTokens.Causation,
            .. usesTimeZone ? [SemanticContextRuntimeTokens.TimeZone] : Array.Empty<string>()
        ];
        return JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                contractRevision = usesTimeZone ? SemanticTypedContextDescriptor.TimeZoneContractRevision : SemanticTypedContextDescriptor.ContractRevision,
                typeKinds = new[] { SemanticContextTypeKinds.Runtime, SemanticContextTypeKinds.Shape, SemanticContextTypeKinds.Model },
                sourceKinds = new[]
                {
                    SemanticContextSourceKinds.ContextContract, SemanticContextSourceKinds.Derived, SemanticContextSourceKinds.Command,
                    SemanticContextSourceKinds.ReadModel, SemanticContextSourceKinds.CurrentEvent, SemanticContextSourceKinds.EventSourceId,
                    SemanticContextSourceKinds.ModelProperty, SemanticContextSourceKinds.ConceptValue, SemanticContextSourceKinds.ValidatedProperty,
                    SemanticContextSourceKinds.ValidatedArtifact, SemanticContextSourceKinds.AuthorizedOperation,
                    SemanticContextSourceKinds.CommandIdentifier, SemanticContextSourceKinds.QueryKey, SemanticContextSourceKinds.Unavailable
                },
                runtimeTokens,
                descriptors = descriptors.Select(descriptor => WithResult(descriptor, new
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
                }))
            },
            _options);
    }

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
