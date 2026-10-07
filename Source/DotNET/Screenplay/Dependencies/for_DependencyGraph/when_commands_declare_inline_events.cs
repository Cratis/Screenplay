// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_commands_declare_inline_events : given.a_model
{
    void Establish() => _source = "module M\n  feature A\n    slice StateChange First\n      command C\n        produces event E\n      screen S\n        action D\n  feature B\n    slice StateChange Second\n      command D\n      projection P\n        from E\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_resolve_the_inline_event() => _graph.Edges.Single(edge => edge.Kind == "usesFactsFrom").Producer.Address.ShouldEqual("M.A.First");
}
