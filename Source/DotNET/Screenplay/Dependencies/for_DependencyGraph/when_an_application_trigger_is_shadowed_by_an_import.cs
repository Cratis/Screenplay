// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_an_application_trigger_is_shadowed_by_an_import : given.a_model
{
    void Establish() => _source = "import Outside.E\ntrigger E\nmodule M\n  feature F\n    slice Automation Consumer\n      reaction R\n        when E\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_resolve_the_imported_event() => _graph.Edges.Single().Kind.ShouldEqual("outsideTheModel");
    [Fact] void should_resolve_the_outside_context() => _graph.Edges.Single().Producer.Address.ShouldEqual("context:Outside");
    [Fact] void should_not_count_the_shadowed_trigger() => _graph.ExcludedReferences.ShouldEqual(0);
    [Fact] void should_mark_the_import_as_used() => _graph.UnusedImports.ShouldBeEmpty();
}
