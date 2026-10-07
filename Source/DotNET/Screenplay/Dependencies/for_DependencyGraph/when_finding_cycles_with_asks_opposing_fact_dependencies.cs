// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_finding_cycles_with_asks_opposing_fact_dependencies : given.a_model
{
    IReadOnlyList<DependencyGroup> _groups;

    void Establish() => _graph = Graph("module M\n  feature A\n    slice StateChange First\n      command C\n        produces event E\n      screen S\n        action D\n  feature B\n    slice StateChange Second\n      command D\n      projection P\n        from E\n");
    void Because() => _groups = _graph.Cycles("feature");

    [Fact] void should_exclude_asks_from_cycles() => _groups.ShouldBeEmpty();
}
