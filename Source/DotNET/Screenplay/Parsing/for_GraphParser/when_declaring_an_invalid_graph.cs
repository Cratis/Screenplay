// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.given;

namespace Cratis.Screenplay.Parsing.for_GraphParser;

public class when_declaring_an_invalid_graph : Specification
{
    static string[] Codes(string graph) =>
        [.. new ScreenplayCompiler().Compile(GraphDocuments.Document(graph)).Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).Select(diagnostic => diagnostic.Code)];

    [Theory]
    [InlineData("graph\n  node A\n    id PersonId key\n")]
    [InlineData("graph G\n  nodes A\n")]
    [InlineData("graph G\n  node\n    id PersonId key\n")]
    [InlineData("graph G\n  node A\n    id PersonId key\n  edge E source A\n")]
    [InlineData("graph G\n  node A\n    id PersonId key\n    123\n")]
    [InlineData("graph G\n  node A\n    id PersonId key\n  edge E source A target A\n    id PersonId key\n")]
    [InlineData("graph G\n  node A\n    id PersonId key\n    other String generated\n")]
    [InlineData("graph G\n")]
    public void should_report_a_malformed_declaration(string graph) => Codes(graph).ShouldContain(DiagnosticCodes.InvalidGraphDeclaration);

    [Theory]
    [InlineData("Address")]
    [InlineData("Address[]")]
    [InlineData("Missing")]
    public void should_refuse_a_property_that_is_not_a_primitive(string type) =>
        Codes($"graph G\n  node A\n    id PersonId key\n    value {type}\n").ShouldContain(DiagnosticCodes.InvalidGraphPropertyType);

    [Theory]
    [InlineData("String")]
    [InlineData("String[]")]
    [InlineData("PersonName")]
    [InlineData("Int optional")]
    [InlineData("DateTime")]
    public void should_accept_a_primitive_a_concept_and_arrays(string type) =>
        Codes($"graph G\n  node A\n    id PersonId key\n    value {type}\n").ShouldNotContain(DiagnosticCodes.InvalidGraphPropertyType);

    [Theory]
    [InlineData("    name String\n")]
    [InlineData("    id PersonId key\n    other GroupId key\n")]
    public void should_require_exactly_one_key(string properties) => Codes($"graph G\n  node A\n{properties}").ShouldContain(DiagnosticCodes.InvalidGraphNodeKey);

    [Fact] void should_refuse_an_unknown_edge_source() => Codes("graph G\n  node A\n    id PersonId key\n  edge E source Nope target A\n").ShouldContain(DiagnosticCodes.UnknownGraphEdgeEndpoint);
    [Fact] void should_refuse_an_unknown_edge_target() => Codes("graph G\n  node A\n    id PersonId key\n  edge E source A target Nope\n").ShouldContain(DiagnosticCodes.UnknownGraphEdgeEndpoint);
    [Fact] void should_refuse_an_identity_that_is_not_an_edge_property() => Codes("graph G\n  node A\n    id PersonId key\n  edge E source A target A identified by nope\n    role String\n").ShouldContain(DiagnosticCodes.UnknownGraphEdgeIdentity);
    [Fact] void should_accept_an_identity_that_is_an_edge_property() => Codes("graph G\n  node A\n    id PersonId key\n  edge E source A target A identified by role\n    role String\n").ShouldBeEmpty();
    [Fact] void should_refuse_a_duplicate_node_label() => Codes("graph G\n  node A\n    id PersonId key\n  node A\n    id PersonId key\n").ShouldContain(DiagnosticCodes.DuplicateGraphElement);
    [Fact] void should_refuse_a_duplicate_edge_type() => Codes("graph G\n  node A\n    id PersonId key\n  edge E source A target A\n  edge E source A target A\n").ShouldContain(DiagnosticCodes.DuplicateGraphElement);
    [Fact] void should_refuse_a_duplicate_property() => Codes("graph G\n  node A\n    id PersonId key\n    id String\n").ShouldContain(DiagnosticCodes.DuplicateGraphElement);
    [Fact] void should_refuse_a_duplicate_graph() => Codes("graph G\n  node A\n    id PersonId key\ngraph G\n  node A\n    id PersonId key\n").ShouldContain(DiagnosticCodes.DuplicateGraphElement);
}
