// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Tool.Mcp.for_McpConnection;

public class when_extracting_explicit_projection_and_constraint_references : Specification
{
    string[] _names = [];
    string[] _roles = [];

    void Because()
    {
        SyntaxNode[] nodes =
        [
            new RemoveWithSyntax("Removed", null, null, SourceLocation.Start),
            new RemoveViaJoinSyntax("JoinedRemoval", null, SourceLocation.Start),
            new UniquePropertyConstraintSyntax("UniqueName", "name", "Named", SourceLocation.Start),
            new CompositeKeySyntax("Identity", [], SourceLocation.Start)
        ];
        var references = nodes.SelectMany(node => McpReferenceKinds.For(node)).ToArray();
        _names = [.. references.Select(reference => reference.Name)];
        _roles = [.. references.Select(reference => reference.Role)];
    }

    [Fact] void should_include_every_declared_reference_form() => _names.ShouldContainOnly("Removed", "JoinedRemoval", "Named", "Identity");
    [Fact] void should_retain_the_distinct_roles() => _roles.ShouldContainOnly("remove", "removeViaJoin", "uniqueProperty", "compositeKeyType");
}
