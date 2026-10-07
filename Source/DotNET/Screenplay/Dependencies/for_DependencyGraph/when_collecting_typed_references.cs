// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_collecting_typed_references : given.a_model
{
    [Theory]
    [InlineData("projection P\n        from E", "usesFactsFrom", "from")]
    [InlineData("projection P\n        join value on id\n          with E", "usesFactsFrom", "join")]
    [InlineData("projection P\n        remove with E", "usesFactsFrom", "remove")]
    [InlineData("projection P\n        clear with E", "usesFactsFrom", "clear")]
    [InlineData("projection P\n        remove via join on E", "usesFactsFrom", "removeViaJoin")]
    [InlineData("projection P\n        children items identified by id\n          from E", "usesFactsFrom", "from")]
    [InlineData("projection P\n        nested item\n          from E", "usesFactsFrom", "from")]
    [InlineData("projection P\n        variant Variant\n          enters on E\n          from E", "usesFactsFrom", "entersOn")]
    [InlineData("reducer Fold => View\n        on E", "usesFactsFrom", "reduces")]
    [InlineData("constraint Unique\n        unique event E", "usesFactsFrom", "uniqueEvent")]
    [InlineData("constraint Unique\n        unique id on E", "usesFactsFrom", "uniqueProperty")]
    [InlineData("command D\n        concurrency\n          events E", "usesFactsFrom", "concurrency")]
    [InlineData("command D\n        reads R", "decidesFrom", "reads")]
    [InlineData("reaction React\n        when E\n          invokes C", "reactsTo", "trigger")]
    [InlineData("reaction React\n        when E\n          invokes C", "asks", "invokes")]
    [InlineData("screen V\n        action C", "asks", "action")]
    [InlineData("screen V\n        data R via query Q", "shows", "dataQuery")]
    [InlineData("screen V\n        action C\n          navigate to S", "shows", "navigate")]
    [InlineData("specification T\n        given E\n        when C\n        then E", "verifiedWith", "givenEvent")]
    public void should_collect_typed_references(string body, string kind, string role)
    {
        _graph = Graph(Producer + "  feature B\n    slice StateView Consumer\n      " + body + "\n");
        _graph.Edges.Any(edge => edge.Consumer.Address == "M.B.Consumer" && edge.Producer.Address == "M.A.Producer" && edge.Kind == kind && edge.Evidence.Any(item => item.Role == role)).ShouldBeTrue();
    }
}
