// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpIndex;

public class when_indexing_reaction_trigger_reads : Specification
{
    McpSyntaxIndex _index = null!;

    void Because() => _index = new McpSnapshot([given.synthetic_model.Document("reaction-reads", """
        module Orders
          feature Handling
            slice StateView Status
              readmodel OrderStatus
            slice Automation Handle
              reaction HandleOrder
                every 15 minutes
                  reads OrderStatus
        """)]).Index;

    [Fact] void should_index_the_view_as_an_explicit_read_reference() =>
        _index.References.Count(reference => reference.Role == "reads" && reference.Name == "OrderStatus").ShouldEqual(1);

    [Fact] void should_resolve_the_read_to_the_declared_view() =>
        _index.Resolve(_index.References.Single(reference => reference.Role == "reads")).Single().Kind.ShouldEqual("ReadModel");
}
