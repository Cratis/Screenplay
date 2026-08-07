// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>reactor</c> declaration - reacts to events and produces side effects.
/// </summary>
/// <param name="Name">The name of the reactor.</param>
/// <param name="Triggers">The <see cref="ReactorTriggerSyntax">triggers</see> the reactor reacts to.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional human readable description of what the reactor does.</param>
public record ReactorSyntax(
    string Name,
    IEnumerable<ReactorTriggerSyntax> Triggers,
    SourceLocation Location,
    string? Description = null) : SyntaxNode(Location);

/// <summary>
/// Represents an <c>on &lt;event&gt;</c> trigger within a reactor, with its optional implementation.
/// </summary>
/// <param name="Event">The name of the event that triggers the reactor.</param>
/// <param name="File">The <see cref="FileReferenceSyntax"/> when the implementation lives in an external file.</param>
/// <param name="Code">The <see cref="CodeBlockSyntax"/> when the implementation is declared inline.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional human readable description of what the trigger does.</param>
/// <param name="Produces">The <see cref="ProducesSyntax">events</see> the reaction appends.</param>
/// <param name="Invokes">The <see cref="InvokesSyntax">commands</see> the reaction invokes.</param>
/// <remarks>
/// A trigger with no body is a complete statement of intent - this reactor observes this event. The
/// <c>file</c> reference and the inline code block are realization metadata attached once a slice is
/// implemented, never a precondition for describing the reaction.
/// <para>
/// What the reaction <em>does</em> is a different thing from how it is implemented. A reactor that appends
/// events and dispatches commands used to show neither, so a document could say an automation existed
/// without saying what it set off - the arrows out of an automation were invisible.
/// </para>
/// </remarks>
public record ReactorTriggerSyntax(
    string Event,
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
