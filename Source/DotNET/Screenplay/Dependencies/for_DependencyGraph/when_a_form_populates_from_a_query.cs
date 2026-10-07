// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_a_form_populates_from_a_query : given.a_model
{
    void Establish() => _source = Producer + "  form Edit for C\n    populate via query Q\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_not_invent_a_slice_owner_for_the_module_form() => _graph.Edges.ShouldBeEmpty();
}
