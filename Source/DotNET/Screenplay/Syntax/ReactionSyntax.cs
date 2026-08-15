// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>reaction</c> declaration - behavior that runs when something happens.
/// </summary>
/// <param name="Name">The name of the reaction.</param>
/// <param name="Triggers">The <see cref="ReactionTriggerSyntax">triggers</see> that set the reaction off.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional human readable description of what the reaction does.</param>
/// <param name="Where">The optional <see cref="ConditionSyntax"/> narrowing which occurrences run the reaction.</param>
/// <remarks>
/// <c>reaction</c> rather than <c>reactor</c>, because <em>reactor</em> is Chronicle's event observer and says
/// the trigger is always a domain event. A reaction is the behavior; a trigger is what causes it, and the
/// reaction does not need to know whether that trigger came from the event store, a clock or an integration.
/// </remarks>
public record ReactionSyntax(
    string Name,
    IEnumerable<ReactionTriggerSyntax> Triggers,
    SourceLocation Location,
    string? Description = null,
    ConditionSyntax? Where = null) : SyntaxNode(Location);

/// <summary>
/// Represents one trigger clause within a reaction - what sets it off, and what it does when set off.
/// </summary>
/// <param name="Source">The <see cref="TriggerSourceSyntax"/> naming what causes the reaction to run.</param>
/// <param name="Data">The <see cref="TriggerDataSyntax">values</see> of the occurrence the reaction uses.</param>
/// <param name="File">The <see cref="FileReferenceSyntax"/> when the implementation lives in an external file.</param>
/// <param name="Code">The <see cref="CodeBlockSyntax"/> when the implementation is declared inline.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional human readable description of what the trigger does.</param>
/// <param name="Produces">The <see cref="ProducesSyntax">events</see> the reaction appends.</param>
/// <param name="Invokes">The <see cref="InvokesSyntax">commands</see> the reaction invokes.</param>
/// <remarks>
/// A trigger with no body is a complete statement of intent - this reaction runs when that happens. The
/// <c>file</c> reference and the inline code block are realization metadata attached once a slice is
/// implemented, never a precondition for describing the reaction.
/// </remarks>
public record ReactionTriggerSyntax(
    TriggerSourceSyntax Source,
    IEnumerable<TriggerDataSyntax> Data,
    FileReferenceSyntax? File,
    CodeBlockSyntax? Code,
    SourceLocation Location,
    string? Description = null,
    IEnumerable<ProducesSyntax>? Produces = null,
    IEnumerable<InvokesSyntax>? Invokes = null) : SyntaxNode(Location);

/// <summary>
/// Represents an <c>invokes &lt;Command&gt;</c> declaration - a command a reaction dispatches.
/// </summary>
/// <param name="Command">The name of the command being invoked.</param>
/// <param name="Mappings">The <see cref="PropertyMappingSyntax">mappings</see> filling the command.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// <c>invokes</c> rather than <c>produces</c> because a command is not produced - it is asked for. An event
/// is a fact the reaction appends; a command is an intent it hands to something else, which may still
/// reject it. Using one word for both would say those are the same kind of consequence.
/// </remarks>
public record InvokesSyntax(
    string Command,
    IEnumerable<PropertyMappingSyntax> Mappings,
    SourceLocation Location) : SyntaxNode(Location);
