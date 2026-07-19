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
/// Represents an <c>on &lt;event&gt;</c> trigger within a reactor, with its implementation.
/// </summary>
/// <param name="Event">The name of the event that triggers the reactor.</param>
/// <param name="File">The <see cref="FileReferenceSyntax"/> when the implementation lives in an external file.</param>
/// <param name="Code">The <see cref="CodeBlockSyntax"/> when the implementation is declared inline.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ReactorTriggerSyntax(
    string Event,
    FileReferenceSyntax? File,
    CodeBlockSyntax? Code,
    SourceLocation Location) : SyntaxNode(Location);
