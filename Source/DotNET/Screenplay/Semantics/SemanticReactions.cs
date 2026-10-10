// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines what can set a reaction off in ESM v6.
/// </summary>
public enum SemanticReactionTriggerKind
{
    /// <summary>
    /// An unknown kind. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// An appended event.
    /// </summary>
    Event = 0,

    /// <summary>
    /// An application trigger the document declares.
    /// </summary>
    ApplicationTrigger = 1,

    /// <summary>
    /// The built-in trigger that fires when the application starts.
    /// </summary>
    Startup = 2,

    /// <summary>
    /// The built-in trigger that fires when the application shuts down.
    /// </summary>
    Shutdown = 3,

    /// <summary>
    /// The clock, on a fixed interval counted from the Unix epoch.
    /// </summary>
    Interval = 4,

    /// <summary>
    /// The clock, at a time of day - every day, on a day of the week or on a day of the month, in UTC.
    /// </summary>
    Schedule = 5
}

/// <summary>
/// Represents an application trigger: a named signal the domain does not raise itself, with the values it hands a reaction.
/// </summary>
/// <param name="Id">The trigger semantic identity.</param>
/// <param name="Name">The trigger name.</param>
/// <param name="Properties">The typed values an occurrence of the trigger carries.</param>
public sealed record SemanticApplicationTrigger(SemanticId Id, string Name, ImmutableArray<SemanticProperty> Properties);

/// <summary>
/// Represents a command a reaction asks for.
/// </summary>
/// <param name="Command">The invoked command semantic identity.</param>
/// <param name="Mappings">The command values, from the occurrence that set the reaction off.</param>
public sealed record SemanticInvocation(SemanticId Command, ImmutableArray<SemanticPropertyMapping> Mappings);

/// <summary>
/// Represents one trigger of a reaction and what that occurrence sets off.
/// </summary>
/// <param name="Kind">What sets the reaction off.</param>
public sealed record SemanticReactionTrigger(SemanticReactionTriggerKind Kind)
{
    /// <summary>
    /// Gets the event contract or application trigger that sets the reaction off; unset for built-in and clock triggers.
    /// </summary>
    public SemanticId Source { get; init; }

    /// <summary>
    /// Gets the interval in seconds for an <see cref="SemanticReactionTriggerKind.Interval"/> trigger.
    /// </summary>
    public long? Every { get; init; }

    /// <summary>
    /// Gets the second of the UTC day a <see cref="SemanticReactionTriggerKind.Schedule"/> trigger fires at.
    /// </summary>
    public int? At { get; init; }

    /// <summary>
    /// Gets the day of the week a weekly schedule fires on, Sunday being 0.
    /// </summary>
    public int? OnDayOfWeek { get; init; }

    /// <summary>
    /// Gets the day of the month a monthly schedule fires on.
    /// </summary>
    public int? OnDayOfMonth { get; init; }

    /// <summary>
    /// Gets the condition an occurrence must meet for the reaction to run, over the occurrence's values.
    /// </summary>
    public SemanticCondition? Where { get; init; }

    /// <summary>
    /// Gets the events the reaction appends, in order, before it invokes commands.
    /// </summary>
    public ImmutableArray<SemanticProducedEvent> Produces { get; init; } = [];

    /// <summary>
    /// Gets the commands the reaction asks for, in order.
    /// </summary>
    public ImmutableArray<SemanticInvocation> Invokes { get; init; } = [];

    /// <summary>
    /// Gets the implementation requirement of an opaque reaction body, which a target provides.
    /// </summary>
    public string? RequirementId { get; init; }
}

/// <summary>
/// Represents a reaction: behavior that runs when something happens.
/// </summary>
/// <param name="Id">The reaction semantic identity.</param>
/// <param name="Name">The reaction name.</param>
/// <param name="Triggers">The triggers in authored order.</param>
public sealed record SemanticReaction(SemanticId Id, string Name, ImmutableArray<SemanticReactionTrigger> Triggers)
{
    /// <summary>
    /// Gets the event source and optional stream filter.
    /// </summary>
    public SemanticObserverFilter? From { get; init; }

    /// <summary>
    /// Gets the authorization identity used by invoked commands, independently of audit identity.
    /// </summary>
    public SemanticReactionIdentity? RunsAs { get; init; }
}

/// <summary>
/// Represents the authorization identity under which a reaction invokes commands.
/// </summary>
/// <param name="Kind">The identity kind.</param>
/// <param name="Roles">The distinct, ordinally sorted roles, which may be empty.</param>
public sealed record SemanticReactionIdentity(SemanticReactionIdentityKind Kind, ImmutableArray<string> Roles);
