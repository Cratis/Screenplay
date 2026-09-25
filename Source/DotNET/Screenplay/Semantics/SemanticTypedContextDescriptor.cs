// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>A portable type in a code context: either a model type, a shaped declaration, or a named runtime contract.</summary>
public sealed record SemanticContextType(string Kind, SemanticTypeReference? ModelType, SemanticId? Shape, string? RuntimeToken)
{
    /// <summary>The declared properties of a shaped payload, in model order. Optional members stay distinct from a nullable payload.</summary>
    public ImmutableArray<SemanticContextProperty> Properties { get; init; } = [];
}

/// <summary>A model-declared property on a shaped payload.</summary>
public sealed record SemanticContextProperty(string Name, SemanticId Id, SemanticTypeReference Type);

/// <summary>Where a context member obtains its value. Runtime and derived values do not masquerade as model properties.</summary>
public sealed record SemanticContextSource(string Kind, SemanticId? SemanticId, string Path);

/// <summary>One data or derived property of a context (not an accessor method).</summary>
public sealed record SemanticTypedContextMember(string Name, SemanticContextType Type, bool IsNullable, bool IsDerived, SemanticContextSource Source);

/// <summary>A wrapper-ready context for one requirement and, for policies, one authorized operation.</summary>
public sealed record SemanticTypedContextDescriptor(
    string RequirementId,
    SemanticImplementationRole Role,
    uint ContextVersion,
    SemanticId? OperationId,
    ImmutableArray<SemanticTypedContextMember> Members)
{
    /// <summary>Independent version of this sidecar contract; not an ESM schema or attachment revision.</summary>
    public const uint ContractRevision = 1;

    /// <summary>Revision of the ESM produced in the same compilation; null for failed compilations such as unbound handlers.</summary>
    public SemanticRevision? ModelRevision { get; init; }
}
