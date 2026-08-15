// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines the units an interval trigger counts in.
/// </summary>
public enum IntervalUnit
{
    /// <summary>
    /// Seconds.
    /// </summary>
    Seconds = 0,

    /// <summary>
    /// Minutes.
    /// </summary>
    Minutes = 1,

    /// <summary>
    /// Hours.
    /// </summary>
    Hours = 2,

    /// <summary>
    /// Days.
    /// </summary>
    Days = 3
}

/// <summary>
/// Represents a <c>trigger</c> declaration - a kind of occurrence a reaction can respond to, and the values
/// it hands the reaction.
/// </summary>
/// <param name="Name">The name of the trigger.</param>
/// <param name="Data">The <see cref="TriggerDataSyntax">values</see> an occurrence of this trigger provides.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional human readable description of when the trigger occurs.</param>
/// <remarks>
/// A declared trigger is how the language stays open. The compiler knows the trigger exists and what it
/// provides; it never knows what makes one occur - that belongs to whatever produces it, be it an
/// integration, a runtime signal or a person. That boundary is what lets the set of triggers be extended
/// without extending the language.
/// </remarks>
public record TriggerSyntax(
    string Name,
    IEnumerable<TriggerDataSyntax> Data,
    SourceLocation Location,
    string? Description = null) : SyntaxNode(Location);

/// <summary>
/// Represents one value a trigger provides, or one a reaction takes from the occurrence.
/// </summary>
/// <param name="Name">The name of the value.</param>
/// <param name="Type">The <see cref="TypeRefSyntax"/> of the value, or <c>null</c> when it is left unstated.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// The type is optional. A trigger declaration is often written before the shape of what it carries is
/// settled, and a reaction that lists what it uses is selecting from a shape someone else already declared
/// rather than declaring one of its own.
/// </remarks>
public record TriggerDataSyntax(
    string Name,
    TypeRefSyntax? Type,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents what causes a reaction to run.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record TriggerSourceSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>when &lt;Name&gt;</c> trigger - an event, a declared trigger, or one a consumer registered.
/// </summary>
/// <param name="Name">The name of the event or trigger.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// One word for all of them on purpose. A reaction says what it responds to; whether that turns out to be a
/// domain event, a message from an integration or a signal the host raises is the trigger's business, not
/// the reaction's.
/// </remarks>
public record NamedTriggerSourceSyntax(string Name, SourceLocation Location) : TriggerSourceSyntax(Location);

/// <summary>
/// Represents an <c>every &lt;n&gt; &lt;unit&gt;</c> trigger - a reaction that runs on an interval.
/// </summary>
/// <param name="Amount">How many <paramref name="Unit"/> pass between runs.</param>
/// <param name="Unit">The <see cref="IntervalUnit"/> the amount is counted in.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record IntervalTriggerSourceSyntax(int Amount, IntervalUnit Unit, SourceLocation Location) : TriggerSourceSyntax(Location);

/// <summary>
/// Represents an <c>at &lt;time&gt;</c> trigger - a reaction that runs at a time of day, optionally narrowed
/// to a day of the week or a day of the month.
/// </summary>
/// <param name="Time">The time of day the reaction runs at.</param>
/// <param name="DayOfWeek">The day of the week it runs on, or <c>null</c> when it runs every day.</param>
/// <param name="DayOfMonth">The day of the month it runs on, or <c>null</c> when it runs every day.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// A time with no qualifier is every day. Saying a day of the week and a day of the month at once has no
/// occurrence in most months, so the language accepts one or the other.
/// </remarks>
public record ScheduleTriggerSourceSyntax(
    TimeOnly Time,
    DayOfWeek? DayOfWeek,
    int? DayOfMonth,
    SourceLocation Location) : TriggerSourceSyntax(Location);
