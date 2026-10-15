// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents the <c>sample data</c> block of a <see cref="UiProfileSyntax"/>: which sample data the whole application previews with.
/// </summary>
/// <param name="Active">The active pack, or <c>null</c> to fall through to the blueprint's default pack and then generated data.</param>
/// <param name="Mappings">The <c>shape ... maps to ...</c> lines, for where a pack's shape name is not the read model's name.</param>
/// <param name="Bindings">The <c>bind ... to ...</c> lines, each a rare override of the active pack for one read model.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// The choice is made once, here, for the whole application and inherited everywhere: there is no per-screen, per-slice or
/// per-component selector.
/// </remarks>
public record SampleDataSelectionSyntax(
    SampleDataPackReferenceSyntax? Active,
    IEnumerable<SampleDataMappingSyntax> Mappings,
    IEnumerable<SampleDataBindingSyntax> Bindings,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a reference to a sample data pack: one declared in the document, or one a blueprint ships.
/// </summary>
/// <param name="Name">The pack's name in the document, or the blueprint pack's id (<c>crm-nordic-smb</c>).</param>
/// <param name="IsBlueprint">Whether the reference is <c>blueprint &lt;id&gt;</c> rather than a pack declared in the document.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SampleDataPackReferenceSyntax(string Name, bool IsBlueprint, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>shape &lt;Shape&gt; maps to &lt;ReadModel&gt;</c> line.
/// </summary>
/// <param name="Shape">The pack shape's name.</param>
/// <param name="ReadModel">The read model it supplies rows for.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SampleDataMappingSyntax(string Shape, string ReadModel, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>bind &lt;ReadModel&gt; to &lt;pack&gt;</c> line: the pack one read model previews with, whatever the active pack is.
/// </summary>
/// <param name="ReadModel">The read model (or shape) name.</param>
/// <param name="Pack">The pack it is bound to.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SampleDataBindingSyntax(string ReadModel, SampleDataPackReferenceSyntax Pack, SourceLocation Location) : SyntaxNode(Location);
