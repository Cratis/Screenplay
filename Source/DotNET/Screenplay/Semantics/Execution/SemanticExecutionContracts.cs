// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Defines normalized portable execution outcomes.
/// </summary>
public enum SemanticExecutionOutcomeKind
{
    /// <summary>
    /// The command and requested queries completed atomically.
    /// </summary>
    Accepted = 0,

    /// <summary>
    /// The command was rejected without changing the world.
    /// </summary>
    Rejected = 1,

    /// <summary>
    /// The decision must be reconsidered from fresh state.
    /// </summary>
    Conflict = 2,

    /// <summary>
    /// The plan or execution host lacks a required capability.
    /// </summary>
    Unsupported = 3
}

/// <summary>
/// Defines portable conflict categories.
/// </summary>
public enum SemanticConflictCategory
{
    /// <summary>
    /// The decision state changed after it was read.
    /// </summary>
    DecisionStateChanged = 0
}

/// <summary>
/// Defines portable capabilities that can be unavailable at execution time.
/// </summary>
public enum SemanticExecutionCapability
{
    /// <summary>
    /// Command dispatch for the requested semantic identity.
    /// </summary>
    Command = 0,

    /// <summary>
    /// Query dispatch for the requested semantic identity.
    /// </summary>
    Query = 1,

    /// <summary>
    /// Deterministic allocation of a produced event destination.
    /// </summary>
    IdentityAllocation = 2,

    /// <summary>
    /// Projection of a complete keyed read-model state.
    /// </summary>
    Projection = 3
}

/// <summary>
/// Defines portable rejection categories.
/// </summary>
public enum SemanticRejectionCategory
{
    /// <summary>
    /// The request does not match its command contract.
    /// </summary>
    Contract = 0,

    /// <summary>
    /// Declarative validation rejected the command.
    /// </summary>
    Validation = 1
}

/// <summary>
/// Represents one fact produced by portable execution.
/// </summary>
/// <param name="EventContract">The event declaration semantic identity.</param>
/// <param name="Destination">The modeled event-source destination.</param>
/// <param name="Values">The event values in target-property order.</param>
public sealed record SemanticFact(
    SemanticId EventContract,
    SemanticValue Destination,
    ImmutableArray<SemanticPropertyValue> Values);

/// <summary>
/// Represents one keyed read-model instance in the portable world.
/// </summary>
/// <param name="ReadModel">The read-model semantic identity.</param>
/// <param name="Key">The modeled runtime key.</param>
/// <param name="Values">The current property values.</param>
public sealed record SemanticReadModelInstance(
    SemanticId ReadModel,
    SemanticValue Key,
    ImmutableArray<SemanticPropertyValue> Values);

/// <summary>
/// Represents one query requested after command execution.
/// </summary>
/// <param name="Query">The query semantic identity.</param>
/// <param name="Key">The query key.</param>
public sealed record SemanticQueryRequest(SemanticId Query, SemanticValue Key);

/// <summary>
/// Represents one normalized query result.
/// </summary>
/// <param name="Query">The query semantic identity.</param>
/// <param name="Key">The query key.</param>
/// <param name="Results">The results in deterministic comparison order.</param>
public sealed record SemanticQueryResult(
    SemanticId Query,
    SemanticValue Key,
    ImmutableArray<SemanticReadModelInstance> Results);

/// <summary>
/// Represents one command execution request.
/// </summary>
/// <param name="Command">The command semantic identity.</param>
/// <param name="Values">The command values.</param>
/// <param name="Queries">Queries to execute against tentative state after the command succeeds.</param>
/// <param name="AllocatedIdentities">Deterministic identities supplied for produced events without an explicit destination, keyed by command identity.</param>
public sealed record SemanticExecutionRequest(
    SemanticId Command,
    ImmutableArray<SemanticPropertyValue> Values,
    ImmutableArray<SemanticQueryRequest> Queries,
    ImmutableDictionary<SemanticId, SemanticValue> AllocatedIdentities)
{
    /// <summary>
    /// Creates a request with no generated identity requirements.
    /// </summary>
    /// <param name="command">The command semantic identity.</param>
    /// <param name="values">The command values.</param>
    /// <param name="queries">Queries to execute after success.</param>
    /// <returns>The request.</returns>
    public static SemanticExecutionRequest Create(
        SemanticId command,
        ImmutableArray<SemanticPropertyValue> values,
        ImmutableArray<SemanticQueryRequest> queries) =>
        new(command, values, queries, []);
}

/// <summary>
/// Represents the base of every normalized execution result.
/// </summary>
/// <param name="Kind">The outcome kind.</param>
/// <param name="World">The resulting world; unchanged for non-accepted outcomes.</param>
public abstract record SemanticExecutionResult(SemanticExecutionOutcomeKind Kind, SemanticWorld World);

/// <summary>
/// Represents an accepted atomic execution.
/// </summary>
/// <param name="World">The resulting committed world.</param>
/// <param name="Facts">The facts produced by this execution.</param>
/// <param name="Queries">The requested query results against tentative committed state.</param>
public sealed record SemanticAccepted(
    SemanticWorld World,
    ImmutableArray<SemanticFact> Facts,
    ImmutableArray<SemanticQueryResult> Queries) : SemanticExecutionResult(SemanticExecutionOutcomeKind.Accepted, World);

/// <summary>
/// Represents a rejected command.
/// </summary>
/// <param name="World">The unchanged world.</param>
/// <param name="Category">The rejection category.</param>
/// <param name="Code">The optional stable rejection code.</param>
/// <param name="Details">Human-readable rejection details.</param>
public sealed record SemanticRejected(
    SemanticWorld World,
    SemanticRejectionCategory Category,
    string? Code,
    string Details) : SemanticExecutionResult(SemanticExecutionOutcomeKind.Rejected, World);

/// <summary>
/// Represents a decision conflict requiring reconsideration.
/// </summary>
/// <param name="World">The unchanged world.</param>
/// <param name="Category">The stable conflict category.</param>
/// <param name="Details">Human-readable conflict details.</param>
public sealed record SemanticConflict(
    SemanticWorld World,
    SemanticConflictCategory Category,
    string Details) : SemanticExecutionResult(SemanticExecutionOutcomeKind.Conflict, World);

/// <summary>
/// Represents a missing execution capability.
/// </summary>
/// <param name="World">The unchanged world.</param>
/// <param name="Capability">The missing capability.</param>
/// <param name="Details">Human-readable capability details.</param>
public sealed record SemanticUnsupported(
    SemanticWorld World,
    SemanticExecutionCapability Capability,
    string Details) : SemanticExecutionResult(SemanticExecutionOutcomeKind.Unsupported, World);
