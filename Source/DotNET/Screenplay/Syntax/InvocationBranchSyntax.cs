// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an ordered result branch on a reaction's invocation: <c>on returned</c>, <c>on &lt;Outcome&gt;</c> or <c>on failed</c>.
/// </summary>
/// <param name="Selector">The branch selector: <c>returned</c>, <c>failed</c> or the name of a declared ability outcome.</param>
/// <param name="Produces">The events the branch produces.</param>
/// <param name="Location">The source location of the branch.</param>
public record InvocationBranchSyntax(
    string Selector,
    IEnumerable<ProducesSyntax> Produces,
    SourceLocation Location) : SyntaxNode(Location);
