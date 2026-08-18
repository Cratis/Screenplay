// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>readmodel</c> declaration - a shape the application reads, standing on its own.
/// </summary>
/// <param name="Name">The name of the read model.</param>
/// <param name="Properties">The <see cref="PropertySyntax">properties</see> the read model holds.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional human readable description of the read model.</param>
/// <param name="File">The <see cref="FileReferenceSyntax"/> naming the file the read model is realized by.
/// It says where the shape lives, which is a different question from where whatever builds it lives - a
/// reducer rule carries its own <c>file</c>.</param>
/// <remarks>
/// A read model declares what it <em>is</em> and never what composes it. Whatever builds it - a
/// <see cref="Projections.ProjectionSyntax">projection</see> or a <see cref="ReducerSyntax">reducer</see> -
/// names it with the <c>=&gt;</c> arrow, so the arrow always points the same way and a reader follows one
/// direction to find out where state comes from.
/// <para>
/// Exactly one thing may build a read model. Two builders would leave a reader, and a runtime, with no answer
/// to which one produced the value in front of them.
/// </para>
/// </remarks>
public record ReadModelSyntax(
    string Name,
    IEnumerable<PropertySyntax> Properties,
    SourceLocation Location,
    string? Description = null,
    FileReferenceSyntax? File = null) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>reducer &lt;Name&gt; =&gt; &lt;ReadModel&gt;</c> declaration - a read model built by
/// folding events into current state, for the views a declarative projection cannot express.
/// </summary>
/// <param name="Name">The name of the reducer.</param>
/// <param name="ReadModel">The read model the reducer builds.</param>
/// <param name="Rules">The <see cref="ReducerRuleSyntax">rules</see> saying how each event changes it.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional human readable description of the reducer.</param>
public record ReducerSyntax(
    string Name,
    string ReadModel,
    IEnumerable<ReducerRuleSyntax> Rules,
    SourceLocation Location,
    string? Description = null) : SyntaxNode(Location);

/// <summary>
/// Represents an <c>on &lt;EventType&gt;</c> rule within a reducer.
/// </summary>
/// <param name="Event">The event the rule folds in.</param>
/// <param name="File">The <see cref="FileReferenceSyntax"/> when the reduction lives in an external file.</param>
/// <param name="Code">The <see cref="CodeBlockSyntax"/> when the reduction is declared inline.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional human readable description of what the rule does.</param>
/// <remarks>
/// A rule with no body states that the reducer folds in the event, which is the part a reader of the document
/// needs. The reduction itself is code by definition - it is what a projection could not say - and it compiles
/// against <see cref="Contexts.ReducerContext"/>.
/// </remarks>
public record ReducerRuleSyntax(
    string Event,
    FileReferenceSyntax? File,
    CodeBlockSyntax? Code,
    SourceLocation Location,
    string? Description = null) : SyntaxNode(Location);
