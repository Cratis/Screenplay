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
public record QuerySyntax(
    string Name,
    TypeRefSyntax ReturnType,
    QueryParameterSyntax? By,
    IEnumerable<QueryParameterSyntax> Filters,
    AuthorizeSyntax? Authorize,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a parameter of a query.
/// </summary>
/// <param name="Name">The name of the parameter.</param>
/// <param name="Type">The <see cref="TypeRefSyntax"/> of the parameter.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record QueryParameterSyntax(string Name, TypeRefSyntax Type, SourceLocation Location) : SyntaxNode(Location);
