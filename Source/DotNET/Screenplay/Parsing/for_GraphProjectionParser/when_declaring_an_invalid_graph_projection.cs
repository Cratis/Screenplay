// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.given;

namespace Cratis.Screenplay.Parsing.for_GraphProjectionParser;

public class when_declaring_an_invalid_graph_projection : Specification
{
    static string[] Codes(string projection, string graph = GraphDocuments.Graph) =>
        [.. new ScreenplayCompiler().Compile(GraphDocuments.Document(graph, GraphDocuments.Events, projection)).Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).Select(diagnostic => diagnostic.Code)];

    static string Body(string body) => $"projection P => graph Organization\n{body}";

    [Fact] void should_accept_the_declared_projection() => Codes(GraphDocuments.Projection).ShouldBeEmpty();

    [Fact] void should_refuse_an_undeclared_graph() => Codes("projection P => graph Nope\n  node Person\n    from PersonRegistered key $eventSourceId\n").ShouldContain(DiagnosticCodes.UnknownGraphProjectionTarget);
    [Fact] void should_refuse_an_undeclared_node() => Codes(Body("  node Nope\n    from PersonRegistered key $eventSourceId\n")).ShouldContain(DiagnosticCodes.UnknownGraphProjectionElement);
    [Fact] void should_refuse_an_undeclared_edge() => Codes(Body("  edge NOPE\n    from MemberAdded source $eventSourceId target groupId\n")).ShouldContain(DiagnosticCodes.UnknownGraphProjectionElement);
    [Fact] void should_refuse_a_mapping_to_an_undeclared_node_property() => Codes(Body("  node Person\n    from PersonRegistered key $eventSourceId\n      nope = name\n")).ShouldContain(DiagnosticCodes.UnknownGraphProperty);
    [Fact] void should_refuse_a_mapping_to_an_undeclared_edge_property() => Codes(Body("  edge MEMBER_OF\n    from MemberAdded source $eventSourceId target groupId\n      nope = 1\n")).ShouldContain(DiagnosticCodes.UnknownGraphProperty);
    [Fact] void should_refuse_a_mapping_to_an_undeclared_property_under_every() => Codes(Body("  node Person\n    every\n      nope = 1\n    from PersonRegistered key $eventSourceId\n")).ShouldContain(DiagnosticCodes.UnknownGraphProperty);
    [Fact] void should_refuse_detach_on_an_edge() => Codes(Body("  edge MEMBER_OF\n    from MemberAdded source $eventSourceId target groupId\n    remove with PersonRemoved source $eventSourceId target groupId detach\n")).ShouldContain(DiagnosticCodes.InvalidGraphEdgeRemoval);
    [Fact] void should_warn_about_a_removal_that_leaves_dangling_edges() => Codes(Body("  node Person\n    from PersonRegistered key $eventSourceId\n    remove with PersonRemoved key $eventSourceId\n")).ShouldContain(DiagnosticCodes.MissingGraphDetach);
    [Fact] void should_not_warn_about_a_detached_removal() => Codes(Body("  node Person\n    from PersonRegistered key $eventSourceId\n    remove with PersonRemoved key $eventSourceId detach\n")).ShouldNotContain(DiagnosticCodes.MissingGraphDetach);
    [Fact] void should_not_warn_when_no_edge_attaches_to_the_label() => Codes(Body("  node Person\n    from PersonRegistered key $eventSourceId\n    remove with PersonRemoved key $eventSourceId\n"), "graph Organization\n  node Person\n    personId PersonId key\n    name PersonName\n").ShouldNotContain(DiagnosticCodes.MissingGraphDetach);
    [Fact] void should_refuse_an_identity_on_an_edge_that_has_none() => Codes(Body("  edge MEMBER_OF\n    from MemberAdded source $eventSourceId target groupId identified by groupId\n")).ShouldContain(DiagnosticCodes.UnknownGraphEdgeIdentity);
    [Fact] void should_require_an_identity_when_the_edge_declares_one() => Codes(Body("  edge HAS\n    from MemberAdded source $eventSourceId target groupId\n"), GraphDocuments.Graph + "  edge HAS source Person target Group identified by tag\n    tag String\n").ShouldContain(DiagnosticCodes.InvalidGraphProjection);
    [Fact] void should_refuse_building_a_node_twice() => Codes(Body("  node Person\n    from PersonRegistered key $eventSourceId\n  node Person\n    from PersonRegistered key $eventSourceId\n")).ShouldContain(DiagnosticCodes.DuplicateGraphElement);
    [Fact] void should_warn_about_an_unknown_event() => Codes(Body("  node Person\n    from Unknown key $eventSourceId\n")).ShouldContain(DiagnosticCodes.UnknownEvent);

    [Theory]
    [InlineData("  children items identified by id\n    from PersonRegistered\n")]
    [InlineData("  nested items\n    from PersonRegistered\n")]
    [InlineData("  join items on id\n    with PersonRegistered\n")]
    [InlineData("  all\n    name = 1\n")]
    [InlineData("  clear with PersonRemoved\n")]
    [InlineData("  variant V\n    enters on PersonRegistered\n")]
    [InlineData("  key $eventSourceId\n")]
    [InlineData("  sequence x\n")]
    [InlineData("  automap\n")]
    [InlineData("  no automap\n")]
    [InlineData("  node Person\n    from PersonRegistered key $eventSourceId\n    children items identified by id\n      from PersonRegistered\n")]
    [InlineData("  node Person\n    from PersonRegistered key $eventSourceId\n      automap\n")]
    public void should_refuse_a_read_model_only_construct(string body) => Codes(Body(body)).ShouldContain(DiagnosticCodes.UnsupportedGraphProjectionConstruct);

    [Theory]
    [InlineData("  node Person\n")]
    [InlineData("  node Person\n    from PersonRegistered\n")]
    [InlineData("  node Person\n    from PersonRegistered source a target b\n")]
    [InlineData("  edge MEMBER_OF\n    from MemberAdded key groupId\n")]
    [InlineData("  edge MEMBER_OF\n    from MemberAdded source groupId\n")]
    [InlineData("  node Person\n    from PersonRegistered key $eventSourceId\n    remove with PersonRemoved\n")]
    [InlineData("  node Person\n    from PersonRegistered key $eventSourceId\n    remove with PersonRemoved source a target b\n")]
    [InlineData("  node Person\n    from PersonRegistered key $eventSourceId\n    unknown\n")]
    [InlineData("  node\n")]
    [InlineData("  thing Person\n")]
    public void should_refuse_a_malformed_block(string body) => Codes(Body(body)).ShouldContain(DiagnosticCodes.InvalidGraphProjection);

    [Fact] void should_refuse_a_projection_without_blocks() => Codes("projection P => graph Organization\n").ShouldContain(DiagnosticCodes.InvalidGraphProjection);

    [Fact]
    void should_refuse_a_graph_projection_in_a_projection_document() =>
        new ScreenplayCompiler().CompileProjection("projection P => graph Organization\n  node Person\n    from PersonRegistered key $eventSourceId\n").Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.GraphProjectionOutsideSlice);
}
