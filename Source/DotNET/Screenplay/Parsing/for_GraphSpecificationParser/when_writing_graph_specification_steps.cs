// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.given;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing.for_GraphSpecificationParser;

public class when_writing_graph_specification_steps : Specification
{
    const string Person = "\"6fa459ea-ee8a-4ca4-894e-db77e160355e\"";
    const string Group = "\"0f8fad5b-d9cb-469f-a165-70867728950e\"";

    static (SpecificationSyntax? Specification, string[] Codes) Compile(string steps)
    {
        var specification = $"specification S\n  given PersonRegistered\n    for {Person}\n    name = \"Ada\"\n{steps}";
        var result = new ScreenplayCompiler().Compile(GraphDocuments.Document(GraphDocuments.Graph, GraphDocuments.Events, GraphDocuments.Projection, specification));
        var codes = result.Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).Select(diagnostic => diagnostic.Code).ToArray();
        return (result.Value?.Modules.Single().Features.Single().Slices.Single().Specifications.SingleOrDefault(), codes);
    }

    [Fact]
    void should_read_a_node_assertion_with_properties()
    {
        var (specification, codes) = Compile($"  when append PersonRemoved\n    for {Person}\n  then graph Organization\n    has node Person {Person}\n      name = \"Ada\"\n");
        codes.ShouldBeEmpty();
        var node = (SpecificationGraphNodeSyntax)specification!.ThenGraphs.Single().Elements.Single();
        node.Label.ShouldEqual("Person");
        node.Properties.Single().Property.ShouldEqual("name");
        node.Absent.ShouldBeFalse();
    }

    [Fact]
    void should_read_an_absent_edge()
    {
        var (specification, codes) = Compile($"  when append PersonRemoved\n    for {Person}\n  then graph Organization\n    no edge MEMBER_OF from {Person} to {Group}\n");
        codes.ShouldBeEmpty();
        ((SpecificationGraphEdgeSyntax)specification!.ThenGraphs.Single().Elements.Single()).Absent.ShouldBeTrue();
    }

    [Fact]
    void should_read_a_seeded_graph()
    {
        var (specification, codes) = Compile($"  given graph Organization\n    has node Person {Person}\n  when append PersonRemoved\n    for {Person}\n  then graph Organization\n    no node Person {Person}\n");
        codes.ShouldBeEmpty();
        specification!.GivenGraphs.Single().Graph.ShouldEqual("Organization");
    }

    [Fact]
    void should_read_an_edge_identity()
    {
        var (specification, _) = Compile($"  when append PersonRemoved\n    for {Person}\n  then graph Organization\n    has edge MEMBER_OF from {Person} to {Group} identified by \"a1\"\n");
        ((SpecificationGraphEdgeSyntax)specification!.ThenGraphs.Single().Elements.Single()).Identity.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("  then graph\n    has node Person \"p\"\n")]
    [InlineData("  then graph Organization\n")]
    [InlineData("  then graph Organization\n    node Person \"p\"\n")]
    [InlineData("  then graph Organization\n    has node Person\n")]
    [InlineData("  then graph Organization\n    has edge MEMBER_OF from \"a\"\n")]
    [InlineData("  then graph Organization\n    has edge MEMBER_OF from \"a\" to \"b\" identified \"c\"\n")]
    [InlineData("  then graph Organization\n    no node Person \"p\"\n      name = \"Ada\"\n")]
    [InlineData("  given graph Organization\n    no node Person \"p\"\n")]
    public void should_refuse_a_malformed_step(string steps) => Compile($"  when append PersonRemoved\n    for {Person}\n{steps}").Codes.ShouldContain(DiagnosticCodes.InvalidGraphSpecification);

    [Fact] void should_refuse_an_undeclared_graph() => Compile("  when append PersonRemoved\n    for \"p\"\n  then graph Nope\n    has node Person \"p\"\n").Codes.ShouldContain(DiagnosticCodes.UnknownGraphSpecificationReference);
    [Fact] void should_refuse_an_undeclared_node() => Compile("  when append PersonRemoved\n    for \"p\"\n  then graph Organization\n    has node Nope \"p\"\n").Codes.ShouldContain(DiagnosticCodes.UnknownGraphSpecificationReference);
    [Fact] void should_refuse_an_undeclared_edge() => Compile("  when append PersonRemoved\n    for \"p\"\n  then graph Organization\n    has edge NOPE from \"p\" to \"q\"\n").Codes.ShouldContain(DiagnosticCodes.UnknownGraphSpecificationReference);
    [Fact] void should_refuse_an_undeclared_property() => Compile("  when append PersonRemoved\n    for \"p\"\n  then graph Organization\n    has node Person \"p\"\n      nope = 1\n").Codes.ShouldContain(DiagnosticCodes.UnknownGraphProperty);
}
