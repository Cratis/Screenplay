// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines the clock a clock operand reads (decision 0106).
/// </summary>
public enum SemanticClockKind
{
    /// <summary>
    /// The local date of the evaluation instant in the effective calendar (a <c>Date</c>).
    /// </summary>
    Today,

    /// <summary>
    /// The evaluation instant (a <c>DateTime</c>); it does not depend on a calendar.
    /// </summary>
    Now
}

/// <summary>
/// Defines the unit of a clock offset.
/// </summary>
public enum SemanticClockUnit
{
    /// <summary>
    /// A calendar day for <c>today</c>; an exact 24-hour duration for <c>now</c>.
    /// </summary>
    Day,

    /// <summary>
    /// An exact one-hour duration.
    /// </summary>
    Hour,

    /// <summary>
    /// An exact one-minute duration.
    /// </summary>
    Minute
}

/// <summary>
/// A clock operand: <c>today</c> or <c>now</c> with an optional signed offset, optionally reduced to its date.
/// </summary>
/// <param name="Kind">The clock.</param>
/// <param name="Amount">The signed whole offset; zero when there is none.</param>
/// <param name="Unit">The offset unit; meaningful only when the amount is not zero.</param>
public sealed record SemanticClockOperand(SemanticClockKind Kind, int Amount, SemanticClockUnit Unit)
{
    /// <summary>
    /// Gets a value indicating that the operand is the <c>Date</c> of the offset instant (<c>date of now + 2 hours</c>).
    /// </summary>
    public bool DateOf { get; init; }
}

/// <summary>
/// The application time zone (decision 0106).
/// </summary>
/// <param name="Id">The IANA time zone id.</param>
/// <param name="Configurable">Whether the deployment may set the zone through host configuration.</param>
public sealed record SemanticTimeZone(string Id, bool Configurable);

/// <summary>
/// A command calendar: a literal IANA id, or the trusted typed-context member that supplies one (exactly one is set).
/// </summary>
/// <param name="TimeZone">The literal IANA time zone id, or null.</param>
/// <param name="ContextMember">The typed-context member name that supplies the zone, or null.</param>
public sealed record SemanticCommandCalendar(string? TimeZone, string? ContextMember);
