// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>Closed type-kind vocabulary for typed contexts.</summary>
public static class SemanticContextTypeKinds
{
    public const string Runtime = "runtime";
    public const string Shape = "shape";
    public const string Model = "model";
}

/// <summary>Closed source-kind vocabulary for typed contexts.</summary>
public static class SemanticContextSourceKinds
{
    public const string ContextContract = "context-contract";
    public const string Derived = "derived";
    public const string Command = "command";
    public const string ReadModel = "read-model";
    public const string CurrentEvent = "current-event";
    public const string EventSourceId = "event-source-id";
    public const string ModelProperty = "model-property";
    public const string ConceptValue = "concept-value";
    public const string ValidatedProperty = "validated-property";
    public const string ValidatedArtifact = "validated-artifact";
    public const string AuthorizedOperation = "authorized-operation";
    public const string CommandIdentifier = "command-identifier";
    public const string QueryKey = "query-key";
    public const string Unavailable = "unavailable";
}

/// <summary>Portable tokens for non-model values supplied by the runtime.</summary>
public static class SemanticContextRuntimeTokens
{
    public const string Text = "Text";
    public const string WholeNumber = "WholeNumber";
    public const string Boolean = "Boolean";
    public const string DateTime = "DateTime";
    public const string TenantId = "TenantId";
    public const string Identity = "Identity";
    public const string CausedBy = "CausedBy";
    public const string Causation = "Causation";
}

/// <summary>A portable type in a code context: either a model type, a shaped declaration, or a named runtime contract.</summary>
public sealed record SemanticContextType(string Kind, SemanticTypeReference? ModelType, SemanticId? Shape, string? RuntimeToken)
{
    /// <summary>The declared properties of a shaped payload, in model order. Optional members stay distinct from a nullable payload.</summary>
    public ImmutableArray<SemanticContextProperty> Properties { get; init; } = [];
}

/// <summary>A model-declared property on a shaped payload.</summary>
public sealed record SemanticContextProperty(string Name, SemanticId Id, SemanticTypeReference Type);

/// <summary>Where a context member obtains its value. Runtime and derived values do not masquerade as model properties.</summary>
public sealed record SemanticContextSource(string Kind, SemanticId? SemanticId, string Path)
{
    /// <summary>A literal supplied by the declaration, never interpreted as a source path.</summary>
    public string? ConstantValue { get; init; }

    /// <summary>The current revision of an event contract, when this source is an event payload.</summary>
    public EventContractRevision? EventRevision { get; init; }
}

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

    /// <summary>Whether a provider can render this descriptor without an unresolved model or missing type definition.</summary>
    public bool IsWrapperReady => ModelRevision is not null && HasResolvedTypes;

    /// <summary>Transitive definitions for every referenced concept and composite type, in stable declaration order.</summary>
    public ImmutableArray<SemanticContextTypeDefinition> Types { get; init; } = [];

    /// <summary>Whether all referenced model type definitions were resolved during derivation.</summary>
    internal bool HasResolvedTypes { get; init; }
}

/// <summary>A self-contained definition for a referenced model type.</summary>
public sealed record SemanticContextTypeDefinition(SemanticId Id, string Name, SemanticTypeReferenceKind Kind, SemanticPrimitiveType Primitive, ImmutableArray<SemanticContextProperty> Properties);
