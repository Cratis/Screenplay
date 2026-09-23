// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines what starts an interaction when the trigger is one of the built-in kinds.
/// </summary>
/// <remarks>
/// These are the *interaction* triggers - anonymous, positional, and only ever valid inside an <c>on</c>
/// clause. They are a different concept from the top-level <c>trigger</c> declaration an
/// <c>reaction ... when</c> consumes, which stays a declared application signal. An application trigger whose
/// name collides with one of these kinds is reported, because <c>on &lt;Name&gt;</c> would otherwise be
/// ambiguous.
/// </remarks>
public enum InteractionTriggerKind
{
    /// <summary>
    /// An unknown kind. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The element was activated - clicked, tapped, or confirmed from the keyboard.
    /// </summary>
    Click = 0,

    /// <summary>
    /// The element was activated twice.
    /// </summary>
    DoubleClick = 1,

    /// <summary>
    /// Selection changed in an items control.
    /// </summary>
    Select = 2,

    /// <summary>
    /// A form was submitted and passed the modeled validation.
    /// </summary>
    Submit = 3,

    /// <summary>
    /// A bound value changed.
    /// </summary>
    Change = 4,

    /// <summary>
    /// The element or screen became live.
    /// </summary>
    Load = 5,

    /// <summary>
    /// The element or screen was torn down.
    /// </summary>
    Unload = 6,

    /// <summary>
    /// The screen was navigated to.
    /// </summary>
    Enter = 7,

    /// <summary>
    /// The screen was navigated away from.
    /// </summary>
    Leave = 8
}

/// <summary>
/// Defines how prominently a <c>notify</c> action surfaces its message.
/// </summary>
public enum NotificationLevel
{
    /// <summary>
    /// An unknown level. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Informational.
    /// </summary>
    Info = 0,

    /// <summary>
    /// A warning.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// An error.
    /// </summary>
    Error = 2
}

/// <summary>
/// Represents a behavior - a bundle of interaction trigger to action bindings.
/// </summary>
/// <param name="Name">The behavior's name, or <c>null</c> when it was written inline on what it is attached to.</param>
/// <param name="Parameters">The parameters a <c>uses</c> site supplies arguments for.</param>
/// <param name="Bindings">The trigger to action bindings.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the declaration.</param>
/// <param name="Description">The optional description.</param>
/// <param name="Order">The optional order, deciding where this behavior runs among the others attached to the same thing.</param>
/// <remarks>
/// A named behavior is declared at the top level and attached with <c>uses</c>. An inline <c>on</c> block is
/// the same construct with no name, attached to whatever it was written on. One record, one walker method,
/// one printer case - anonymity is a property rather than a separate kind of node.
/// </remarks>
public record BehaviorSyntax(
    string? Name,
    IEnumerable<BehaviorParameterSyntax> Parameters,
    IEnumerable<InteractionBindingSyntax> Bindings,
    SourceLocation Location,
    string? Description = null,
    int? Order = null) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the optional realization reference for the behavior.
    /// </summary>
    public FileReferenceSyntax? File { get; init; }
}

/// <summary>
/// Represents one parameter of a named behavior.
/// </summary>
/// <param name="Name">The parameter name.</param>
/// <param name="Type">The optional declared type.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the declaration.</param>
public record BehaviorParameterSyntax(
    string Name,
    TypeRefSyntax? Type,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one trigger to actions binding inside a behavior.
/// </summary>
/// <param name="Trigger">What starts it.</param>
/// <param name="Condition">The optional <c>where</c> guard.</param>
/// <param name="Actions">The actions to run, in declared order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the <c>on</c> clause.</param>
public record InteractionBindingSyntax(
    InteractionTriggerSyntax Trigger,
    string? Condition,
    IEnumerable<InteractionActionSyntax> Actions,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents what starts an interaction.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> of the trigger.</param>
public abstract record InteractionTriggerSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one of the built-in interaction trigger kinds.
/// </summary>
/// <param name="Kind">The kind.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the trigger.</param>
public record BuiltInInteractionTriggerSyntax(
    InteractionTriggerKind Kind,
    SourceLocation Location) : InteractionTriggerSyntax(Location);

/// <summary>
/// Represents an interaction started by observing a modeled domain event.
/// </summary>
/// <param name="EventName">The event being observed.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the trigger.</param>
public record EventInteractionTriggerSyntax(
    string EventName,
    SourceLocation Location) : InteractionTriggerSyntax(Location);

/// <summary>
/// Represents an interaction started by an elapsed interval.
/// </summary>
/// <param name="Amount">The amount of time.</param>
/// <param name="Unit">The unit the amount is expressed in.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the trigger.</param>
public record IntervalInteractionTriggerSyntax(
    int Amount,
    IntervalUnit Unit,
    SourceLocation Location) : InteractionTriggerSyntax(Location);

/// <summary>
/// Represents an interaction started by a declared application trigger firing - the bridge from the backend
/// vocabulary into the interaction one.
/// </summary>
/// <param name="TriggerName">The declared application trigger.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the trigger.</param>
public record ApplicationTriggerInteractionTriggerSyntax(
    string TriggerName,
    SourceLocation Location) : InteractionTriggerSyntax(Location);

/// <summary>
/// Represents one declarative effect inside a binding.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> of the action.</param>
/// <remarks>
/// Continuations and arguments live on the base as init-only members so that adding an action kind stays an
/// append rather than a change to every existing record.
/// </remarks>
public abstract record InteractionActionSyntax(SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the <c>with &lt;name&gt; from &lt;binding&gt;</c> arguments supplied to the action.
    /// </summary>
    public IEnumerable<InteractionArgumentSyntax> Arguments { get; init; } = [];

    /// <summary>
    /// Gets the actions that run when this one succeeds.
    /// </summary>
    public IEnumerable<InteractionActionSyntax> OnSuccess { get; init; } = [];

    /// <summary>
    /// Gets the actions that run when this one fails.
    /// </summary>
    public IEnumerable<InteractionActionSyntax> OnFailure { get; init; } = [];

    /// <summary>
    /// Gets the actions that run with a dialog's result. Only <c>open dialog</c> carries these.
    /// </summary>
    public IEnumerable<InteractionActionSyntax> OnResult { get; init; } = [];
}

/// <summary>
/// Represents one argument passed to an action.
/// </summary>
/// <param name="Name">The argument name on the target.</param>
/// <param name="Binding">The binding expression the value comes from.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the argument.</param>
public record InteractionArgumentSyntax(
    string Name,
    string Binding,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents <c>execute &lt;Command&gt;</c>.
/// </summary>
/// <param name="Command">The command to submit.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the action.</param>
public record ExecuteCommandActionSyntax(string Command, SourceLocation Location) : InteractionActionSyntax(Location);

/// <summary>
/// Represents <c>navigate to &lt;Screen&gt;</c>.
/// </summary>
/// <param name="Screen">The screen to navigate to.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the action.</param>
public record NavigateActionSyntax(string Screen, SourceLocation Location) : InteractionActionSyntax(Location);

/// <summary>
/// Represents <c>navigate back</c>.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> of the action.</param>
public record NavigateBackActionSyntax(SourceLocation Location) : InteractionActionSyntax(Location);

/// <summary>
/// Represents <c>open dialog &lt;DialogTemplate&gt;</c>.
/// </summary>
/// <param name="DialogTemplate">The dialog template to open.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the action.</param>
public record OpenDialogActionSyntax(string DialogTemplate, SourceLocation Location) : InteractionActionSyntax(Location);

/// <summary>
/// Represents <c>close dialog</c>.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> of the action.</param>
public record CloseDialogActionSyntax(SourceLocation Location) : InteractionActionSyntax(Location);

/// <summary>
/// Represents <c>refresh &lt;Query&gt;</c>.
/// </summary>
/// <param name="Query">The query to re-run.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the action.</param>
public record RefreshQueryActionSyntax(string Query, SourceLocation Location) : InteractionActionSyntax(Location);

/// <summary>
/// Represents <c>set &lt;target&gt; to &lt;expression&gt;</c>.
/// </summary>
/// <param name="Target">The declared <c>state</c> or <c>accepts</c> name being written.</param>
/// <param name="Value">The value to write.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the action.</param>
public record SetStateActionSyntax(string Target, string Value, SourceLocation Location) : InteractionActionSyntax(Location);

/// <summary>
/// Represents <c>notify &lt;level&gt; "&lt;text&gt;"</c>.
/// </summary>
/// <param name="Level">How prominently to surface it.</param>
/// <param name="Message">The message, either a literal or a <c>$strings.</c> reference.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the action.</param>
public record NotifyActionSyntax(NotificationLevel Level, string Message, SourceLocation Location) : InteractionActionSyntax(Location)
{
    /// <summary>
    /// Gets whether the message was written as a string literal, rather than as a <c>$strings.</c> reference or
    /// a binding the surrounding scope supplies. Kept so the message round-trips in the form it was authored.
    /// </summary>
    public bool MessageIsLiteral { get; init; } = true;
}

/// <summary>
/// Represents <c>confirm "&lt;text&gt;"</c> - a gate on the actions that follow it.
/// </summary>
/// <param name="Message">The message, either a literal or a <c>$strings.</c> reference.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the action.</param>
public record ConfirmActionSyntax(string Message, SourceLocation Location) : InteractionActionSyntax(Location)
{
    /// <summary>
    /// Gets whether the message was written as a string literal, rather than as a <c>$strings.</c> reference or
    /// a binding the surrounding scope supplies. Kept so the message round-trips in the form it was authored.
    /// </summary>
    public bool MessageIsLiteral { get; init; } = true;
}

/// <summary>
/// Represents <c>raise &lt;ApplicationTrigger&gt;</c> - the bridge from the interaction vocabulary back into
/// the backend one.
/// </summary>
/// <param name="Trigger">The declared application trigger to fire.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the action.</param>
public record RaiseTriggerActionSyntax(string Trigger, SourceLocation Location) : InteractionActionSyntax(Location);

/// <summary>
/// Represents <c>uses &lt;Behavior&gt;</c> - attaching a named behavior with its arguments.
/// </summary>
/// <param name="Behavior">The behavior being attached.</param>
/// <param name="Arguments">The arguments supplied for the behavior's parameters.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the clause.</param>
public record UsesBehaviorSyntax(
    string Behavior,
    IEnumerable<BehaviorArgumentSyntax> Arguments,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one argument supplied at a <c>uses</c> site.
/// </summary>
/// <param name="Name">The parameter name being supplied.</param>
/// <param name="Value">The value.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the argument.</param>
public record BehaviorArgumentSyntax(
    string Name,
    string Value,
    SourceLocation Location) : SyntaxNode(Location);
