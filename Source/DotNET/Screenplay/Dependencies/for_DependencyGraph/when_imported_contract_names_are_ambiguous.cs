// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_imported_contract_names_are_ambiguous : given.a_model
{
    void Establish() => _source = "import A.Placed\nimport B.Placed\nmodule M\n  feature F\n    slice StateView Consumer\n      projection P\n        from Placed\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_select_the_first_matching_context() => _graph.Edges.Single().Producer.Address.ShouldEqual("context:A");
    [Fact] void should_report_the_ambiguity() => _graph.Edges.Single().Evidence.Single().Ambiguous.ShouldBeTrue();
    [Fact] void should_keep_the_other_context_as_an_alternative() => _graph.Edges.Single().Evidence.Single().Alternatives.Select(node => node.Address).ShouldEqual(["context:B"]);
    [Fact] void should_count_every_matching_import_as_used() => _graph.UnusedImports.ShouldBeEmpty();
}
