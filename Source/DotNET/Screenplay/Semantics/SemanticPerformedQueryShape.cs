// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Identifies how a caller-supplied argument of a performed query is used.
/// </summary>
public enum SemanticPerformedQueryArgumentKind
{
    /// <summary>A <c>by</c> key, or one part of a composite <c>by</c> key.</summary>
    Key,

    /// <summary>An equality filter on a read-model collection.</summary>
    Filter
}

/// <summary>
/// Describes one caller-supplied argument of a query a performer answers.
/// </summary>
/// <param name="Name">The authored argument name.</param>
/// <param name="Kind">Whether the argument is a key or a filter.</param>
/// <param name="Type">The argument type as the executable model describes types, or <see langword="null"/> when the type is unresolved.</param>
/// <param name="IsOptional">Whether the caller may omit the argument.</param>
public sealed record SemanticPerformedQueryArgument(
    string Name,
    SemanticPerformedQueryArgumentKind Kind,
    SemanticTypeReference? Type,
    bool IsOptional);

/// <summary>
/// Describes the shape of a query a performer answers, so a host can render it without executing it.
/// </summary>
/// <remarks>
/// A performed query is not part of the executable semantic model (decision 0097), so this descriptor is carried only on its
/// <see cref="SemanticImplementationRole.QueryPerformer"/> requirement and never affects canonical model bytes.
/// </remarks>
/// <param name="ReadModelName">The authored name of the returned read model.</param>
/// <param name="ReadModel">The returned read model's semantic identity, or <see langword="null"/> when it is unresolved.</param>
/// <param name="ReadModelAddress">The returned read model's semantic address, or <see langword="null"/> when it is unresolved.</param>
/// <param name="Cardinality">The result cardinality, in the vocabulary of executable-model queries.</param>
/// <param name="Delivery">Whether the result is observable (live) or a snapshot.</param>
/// <param name="Arguments">The caller-supplied <c>by</c> keys followed by the filters, in declaration order.</param>
public sealed record SemanticPerformedQueryShape(
    string ReadModelName,
    SemanticId? ReadModel,
    SemanticAddress? ReadModelAddress,
    SemanticQueryCardinality Cardinality,
    SemanticQueryDelivery Delivery,
    ImmutableArray<SemanticPerformedQueryArgument> Arguments)
{
    /// <summary>
    /// Gets who may call the query, resolved exactly as <see cref="SemanticKeyedQuery.Authorization"/> resolves it for a bound query: the policies the module,
    /// the feature path and the query itself require, combined with and. Policy references are by name into <c>SemanticApplication.Policies</c>, so
    /// a policy that is anonymous access (<c language="csharp">require anonymous</c>, ESM v31) is recognizable there.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> means that no policy applies to the query, as it does for an unrestricted bound query. An unresolved policy
    /// reference is a binding error, so a shape that exists never hides one.
    /// </remarks>
    public SemanticAuthorization? Authorization { get; init; }

    /// <inheritdoc />
    public bool Equals(SemanticPerformedQueryShape? other) => other is not null &&
        ReadModelName == other.ReadModelName && ReadModel == other.ReadModel &&
        EqualityComparer<SemanticAddress?>.Default.Equals(ReadModelAddress, other.ReadModelAddress) &&
        Cardinality == other.Cardinality && Delivery == other.Delivery && Arguments.SequenceEqual(other.Arguments) &&
        EqualityComparer<SemanticAuthorization?>.Default.Equals(Authorization, other.Authorization);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(ReadModelName);
        hash.Add(ReadModel);
        hash.Add(ReadModelAddress);
        hash.Add(Authorization);
        hash.Add(Cardinality);
        hash.Add(Delivery);
        foreach (var argument in Arguments) hash.Add(argument);
        return hash.ToHashCode();
    }
}
