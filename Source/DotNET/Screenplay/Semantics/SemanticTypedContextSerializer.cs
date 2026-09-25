// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics;

/// <summary>Serializes the portable descriptor contract independently of model provenance.</summary>
public static class SemanticTypedContextSerializer
{
    static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    /// <summary>Produces stable UTF-8 JSON bytes suitable for shared contract vectors.</summary>
    public static byte[] Serialize(ImmutableArray<SemanticTypedContextDescriptor> descriptors) => JsonSerializer.SerializeToUtf8Bytes(
        new
        {
            contractRevision = SemanticTypedContextDescriptor.ContractRevision,
            typeKinds = new[] { SemanticContextTypeKinds.Runtime, SemanticContextTypeKinds.Shape, SemanticContextTypeKinds.Model },
            sourceKinds = new[]
            {
                SemanticContextSourceKinds.ContextContract, SemanticContextSourceKinds.Derived, SemanticContextSourceKinds.Command,
                SemanticContextSourceKinds.ReadModel, SemanticContextSourceKinds.CurrentEvent, SemanticContextSourceKinds.EventSourceId,
                SemanticContextSourceKinds.ModelProperty, SemanticContextSourceKinds.ConceptValue, SemanticContextSourceKinds.ValidatedProperty,
                SemanticContextSourceKinds.ValidatedArtifact, SemanticContextSourceKinds.AuthorizedOperation,
                SemanticContextSourceKinds.CommandIdentifier, SemanticContextSourceKinds.QueryKey, SemanticContextSourceKinds.Unavailable
            },
            runtimeTokens = new[]
            {
                SemanticContextRuntimeTokens.Text, SemanticContextRuntimeTokens.WholeNumber, SemanticContextRuntimeTokens.Boolean,
                SemanticContextRuntimeTokens.DateTime, SemanticContextRuntimeTokens.TenantId, SemanticContextRuntimeTokens.Identity,
                SemanticContextRuntimeTokens.CausedBy, SemanticContextRuntimeTokens.Causation
            },
            descriptors = descriptors.Select(descriptor => new
            {
                descriptor.RequirementId,
                role = descriptor.Role.ToString(),
                descriptor.ContextVersion,
                operationId = descriptor.OperationId?.ToString(),
                descriptor.IsWrapperReady,
                types = descriptor.Types.Select(type => new
                {
                    id = type.Id.ToString(), type.Name, kind = type.Kind.ToString(), primitive = type.Primitive.ToString(),
                    properties = type.Properties.Select(property => new { property.Name, id = property.Id.ToString(), type = Reference(property.Type) })
                }),
                members = descriptor.Members.Select(member => new
                {
                    member.Name, member.IsNullable, member.IsDerived,
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
                        member.Source.Kind, semanticId = member.Source.SemanticId?.ToString(), member.Source.Path,
                        member.Source.ConstantValue, eventRevision = member.Source.EventRevision?.ToString()
                    }
                })
            })
        },
        _options);

    static object? Reference(SemanticTypeReference? type) => type is null ? null : new
    {
        kind = type.Kind.ToString(), primitive = type.Primitive.ToString(),
        target = type.Kind == SemanticTypeReferenceKind.Primitive ? null : type.Target.ToString(),
        type.IsCollection, type.IsOptional
    };
}
