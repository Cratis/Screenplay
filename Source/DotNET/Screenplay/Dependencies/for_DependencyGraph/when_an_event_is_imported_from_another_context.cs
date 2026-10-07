// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_an_event_is_imported_from_another_context : given.a_model
{
    void Establish() => _source = "import Outside.E\nimport Unused.U\nmodule M\n  feature F\n    slice StateChange C\n      specification T\n        given E\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_hide_imported_test_only_facts_by_default() => _graph.Implied("slice", "context").Count.ShouldEqual(0);
    [Fact] void should_include_test_only_facts_on_request() => _graph.Implied("slice", "context", includeTestOnly: true).Single().Evidence.Single().TestOnly.ShouldBeTrue();
    [Fact] void should_list_the_unused_contract() => _graph.UnusedImports.SequenceEqual(["Unused.U"]).ShouldBeTrue();
}
