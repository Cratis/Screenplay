// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Indexing;

sealed record AuthoredDeclaration(string Kind, string Name, string[] Scope, SourceLocation Location, string? Description, object? Details, SyntaxNode Syntax)
{
    public string Address => string.Join('.', Scope.Append(Name));

    public IEnumerable<SourceLocation> Locations => Parts.Select(part => part.Location);

    public bool IsImplicit { get; init; }

    public string? Case { get; init; }

    internal List<SyntaxNode> Parts { get; } = [Syntax];

    internal ImmutableArray<ReadOwner> Hierarchy { get; init; } = [];

    internal ReadOwner Owner => new(Kind, Name, Address, Location);
}
