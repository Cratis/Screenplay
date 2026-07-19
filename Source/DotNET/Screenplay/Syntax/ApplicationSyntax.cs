// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents the root of a Screenplay document - the application being described.
/// </summary>
/// <param name="Imports">The <see cref="ImportSyntax">imports</see> declared at the top of the document.</param>
/// <param name="Concepts">The <see cref="ConceptSyntax">concepts</see> declared in the document.</param>
/// <param name="Policies">The <see cref="PolicySyntax">policies</see> declared in the document.</param>
/// <param name="Modules">The <see cref="ModuleSyntax">modules</see> declared in the document.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ApplicationSyntax(
    IEnumerable<ImportSyntax> Imports,
    IEnumerable<ConceptSyntax> Concepts,
    IEnumerable<PolicySyntax> Policies,
    IEnumerable<ModuleSyntax> Modules,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an <c>import</c> of a construct from another module.
/// </summary>
/// <param name="QualifiedName">The fully qualified dotted name being imported.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ImportSyntax(string QualifiedName, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the short name - the last segment of the qualified name.
    /// </summary>
    public string Name => QualifiedName[(QualifiedName.LastIndexOf('.') + 1)..];
}
