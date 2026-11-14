// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.for_Samples;

public class when_running_the_organization_graph_specifications : Semantics.for_SemanticModelBinder.given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;
    IReadOnlyList<SemanticGraphSpecificationResult> _results;

    void Because()
    {
        var root = Directory.GetParent(DocumentationExamples.Root())!.FullName;
        _result = Bind(File.ReadAllText(Path.Combine(root, "Samples/Organization/organization.play")));
        _results = _result.Value is { } compilation ? SemanticGraphRunner.RunAll(compilation.Model) : [];
    }

    [Fact] void should_bind_without_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_select_the_collecting_graph_query_version() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V54);
    [Fact] void should_bind_the_graph() => _result.Value!.Model.Application.Graphs.Single().Nodes.Select(node => node.Label).ShouldEqual("Person", "Group");
    [Fact] void should_run_every_specification() => _results.Count.ShouldEqual(12);
    [Fact] void should_pass_every_specification() => _results.Where(result => !result.Passed).Select(result => $"{result.Name}: {string.Join("; ", result.Failures)}").ShouldBeEmpty();
}
