// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison.for_ModelComparison;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Indexing.for_AuthoringIndex;

public class when_indexing_row_narrowing : Specification
{
    AuthoringIndex _index = null!;

    void Because()
    {
        var snapshot = new AuthoringSnapshot(new Dictionary<string, string> { ["application.play"] = when_comparing_agent_exposure.Narrowed });
        snapshot.Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
        _index = snapshot.Index;
    }

    [Fact]
    void should_resolve_the_narrowing_policy_as_authorization()
    {
        var edge = _index.ResolvedReferences.Single(reference => reference.Reference.Role == "authorizes" && reference.Reference.Name == "VisibleRows");
        edge.Reference.Name.ShouldEqual("VisibleRows");
        edge.Candidates.Single().Kind.ShouldEqual("Policy");
        edge.Candidates.Single().Address.ShouldEqual("VisibleRows");
    }

    [Fact]
    void should_include_the_policy_in_the_exposed_query_dependencies()
    {
        var query = _index.Declarations.Single(declaration => declaration.Kind == "Query");
        _index.Outgoing(query.Owner).Single(reference => reference.Role == "authorizes").Name.ShouldEqual("VisibleRows");
    }

    [Fact]
    void should_cover_the_reference_bearing_narrow_node()
    {
        var narrow = new NarrowSyntax("VisibleRows", _index.Declarations.First().Location);
        ReferenceKinds.Covers(narrow, null).ShouldBeTrue();
        ReferenceKinds.For(narrow).Single().Name.ShouldEqual(narrow.Policy);
    }
}
