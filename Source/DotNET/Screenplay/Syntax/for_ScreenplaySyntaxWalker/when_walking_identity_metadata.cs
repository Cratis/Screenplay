// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker;

public class when_walking_identity_metadata : Specification
{
    string[] _visited;

    void Because()
    {
        var syntax = new ScreenplayCompiler().Parse("identity\n  department String from claim \"department\"\n  organization String from query Q by $identity.id\n  external String\n    file Identity/External.cs").Value!;
        var walker = new Walker();
        walker.VisitApplication(syntax);
        _visited = [.. walker.Visited];
    }

    [Fact] void should_visit_the_block_and_each_detail() => _visited.Count(kind => kind == nameof(IdentityDetailSyntax)).ShouldEqual(3);
    [Fact] void should_visit_the_query_key() => _visited.ShouldContain(nameof(IdentityExpressionSyntax));
    [Fact] void should_visit_the_type() => _visited.Count(kind => kind == nameof(TypeRefSyntax)).ShouldEqual(3);
    [Fact] void should_visit_the_file_reference() => _visited.ShouldContain(nameof(FileReferenceSyntax));

    sealed class Walker : ScreenplaySyntaxWalker
    {
        internal List<string> Visited { get; } = [];
        public override void VisitNode(SyntaxNode node) => Visited.Add(node.GetType().Name);
    }
}
