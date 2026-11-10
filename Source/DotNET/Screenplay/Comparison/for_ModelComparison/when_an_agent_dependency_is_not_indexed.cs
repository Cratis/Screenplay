// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_an_agent_dependency_is_not_indexed : Specification
{
    [Theory]
    [InlineData("Query")]
    [InlineData("ReadModel")]
    [InlineData("Policy")]
    void should_fail_closed_for_an_unrecognized_reference_on_an_operation_or_dependency(string kind)
    {
        var before = Snapshot();
        var after = Snapshot();
        var declaration = after.Index.Declarations.First(candidate => candidate.Kind == kind);
        declaration.Parts.Add(new UnindexedReferenceSyntax("VisibleRows", declaration.Location));

        AgentExposureSignature.Read(after.Index, after.Index.Declarations.Single(candidate => candidate.Kind == "Query")).ShouldBeNull();
        var result = AgentExposureComparison.Compare(before, after);
        result.Coverage.ShouldEqual(AgentExposureCoverage.Incomplete);
        result.HasWidening.ShouldBeNull();
        result.Rows.Single().Change.ShouldEqual(AgentExposureChangeKind.Unknown);
    }

    [Fact]
    void should_inspect_children_unknown_to_the_syntax_walker()
    {
        var node = new UnindexedContainerSyntax(new UnindexedReferenceSyntax("VisibleRows", SourceLocation.Start), SourceLocation.Start);
        AgentReferenceCoverage.Complete(node, node).ShouldBeFalse();
    }

    [Fact]
    void should_not_treat_an_unrecognized_literal_as_a_proven_non_reference()
    {
        var node = new UnindexedReferenceSyntax("some-literal-or-declaration", SourceLocation.Start);
        AgentReferenceCoverage.Complete(node, node).ShouldBeFalse();
    }

    static AuthoringSnapshot Snapshot() => new(new Dictionary<string, string> { ["application.play"] = when_comparing_agent_exposure.Narrowed });

    sealed record UnindexedReferenceSyntax(string Policy, SourceLocation Location) : SyntaxNode(Location);
    sealed record UnindexedContainerSyntax(SyntaxNode Child, SourceLocation Location) : SyntaxNode(Location);
}
