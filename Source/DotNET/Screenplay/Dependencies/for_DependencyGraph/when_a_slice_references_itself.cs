// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_a_slice_references_itself : given.a_model
{
    void Establish() => _source = "module M\n  feature F\n    slice StateView Consumer\n      event Local\n      projection P\n        from Local\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_exclude_the_self_reference() => _graph.Edges.ShouldBeEmpty();
}
