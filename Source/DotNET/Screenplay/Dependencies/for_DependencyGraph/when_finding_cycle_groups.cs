// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_finding_cycle_groups : given.a_model
{
    IReadOnlyList<DependencyGroup> _groups;

    void Establish() => _graph = Graph("module M\n  feature A\n    slice StateView V\n      projection R\n        from E\n  feature B\n    slice StateChange W\n      event E\n      command C\n        reads R\n");
    void Because() => _groups = _graph.Cycles("feature");

    [Fact] void should_keep_members_in_authored_order() => _groups.Single().Members.Select(node => node.Address).ShouldEqual(["M.A", "M.B"]);
}
