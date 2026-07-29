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
public record ReactorSyntax(string Name, IEnumerable<ReactorTriggerSyntax> Triggers, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an <c>on &lt;event&gt;</c> trigger within a reactor, with what it produces and its implementation.
/// </summary>
/// <param name="Event">The name of the event that triggers the reactor.</param>
/// <param name="File">The <see cref="FileReferenceSyntax"/> when the implementation lives in an external file.</param>
/// <param name="Code">The <see cref="CodeBlockSyntax"/> when the implementation is declared inline.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Produces">The <see cref="ReactorProducesSyntax">events</see> the trigger appends as side effects.</param>
/// <param name="Executes">The <see cref="ReactorExecutesSyntax">commands</see> the trigger executes.</param>
/// <remarks>
/// Everything below the trigger is optional. A trigger with nothing under it states that the reactor
/// observes the event; <c>produces</c> and <c>executes</c> complete the declarative description of what it
/// does, and <c>file</c> or an inline block attaches the realization.
/// </remarks>
public record ReactorTriggerSyntax(
    string Event,
    FileReferenceSyntax? File,
    CodeBlockSyntax? Code,
    SourceLocation Location,
    IEnumerable<ReactorProducesSyntax>? Produces = null,
    IEnumerable<ReactorExecutesSyntax>? Executes = null) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>produces &lt;Event&gt;</c> declaration under a reactor trigger - an event the reactor
/// appends as a side effect.
/// </summary>
/// <param name="Event">The name of the produced event.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ReactorProducesSyntax(string Event, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an <c>executes &lt;Command&gt;</c> declaration under a reactor trigger - a command the reactor
/// puts through the command pipeline.
/// </summary>
/// <param name="Command">The name of the executed command.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ReactorExecutesSyntax(string Command, SourceLocation Location) : SyntaxNode(Location);
