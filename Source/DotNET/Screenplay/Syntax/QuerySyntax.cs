// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>query</c> declaration - a read against a read model.
/// </summary>
/// <param name="Name">The name of the query.</param>
/// <param name="ReturnType">The <see cref="TypeRefSyntax"/> the query returns.</param>
/// <param name="By">The optional identifying <see cref="QueryParameterSyntax"/> declared with <c>by</c>.</param>
/// <param name="Filters">The narrowing <see cref="QueryParameterSyntax">parameters</see> declared with <c>filter</c>.</param>
/// <param name="Authorize">The optional <see cref="AuthorizeSyntax"/> for the query.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional human readable description of what the query is for.</param>
/// <param name="Performer">The optional <see cref="PerformerSyntax"/> holding the code that performs the query.</param>
/// <param name="IsObservable">Whether the return type is marked with <c>observable</c>, making the query a live
/// read that keeps pushing as the read model changes rather than answering once.</param>
public record QuerySyntax(
    string Name,
    TypeRefSyntax ReturnType,
    QueryParameterSyntax? By,
    IEnumerable<QueryParameterSyntax> Filters,
    AuthorizeSyntax? Authorize,
    SourceLocation Location,
    string? Description = null,
    PerformerSyntax? Performer = null,
    bool IsObservable = false) : SyntaxNode(Location)
{
    /// <summary>
    /// The marker that qualifies a query's return type as a live one.
    /// </summary>
    public const string ObservableModifier = "observable";
}

/// <summary>
/// Represents a parameter of a query.
/// </summary>
/// <param name="Name">The name of the parameter.</param>
/// <param name="Type">The <see cref="TypeRefSyntax"/> of the parameter.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Source">The optional <see cref="ExpressionSyntax"/> declared with <c>from</c>, supplying the
/// parameter from the surrounding context instead of the caller.</param>
public record QueryParameterSyntax(
    string Name,
    TypeRefSyntax Type,
    SourceLocation Location,
    ExpressionSyntax? Source = null) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>performer</c> declaration - the code that performs a query, as an external file or an
/// inline block in a language such as <c>csharp</c> or <c>sql</c>.
/// </summary>
/// <param name="File">The <see cref="FileReferenceSyntax"/> when the performer lives in an external file.</param>
/// <param name="Code">The <see cref="CodeBlockSyntax"/> when the performer is declared inline.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record PerformerSyntax(FileReferenceSyntax? File, CodeBlockSyntax? Code, SourceLocation Location) : SyntaxNode(Location);
