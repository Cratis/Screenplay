// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner;

public class when_running_the_lifecycle_specifications : Specification
{
    ExecutableSemanticModel _model = null!;
    ImmutableArray<SemanticLifecycleSpecificationResult> _results;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", a_lifecycle_source.Source);
        var result = new SemanticModelBinder(2026).Bind("Billing", new ScreenplayCompiler().Parse(a_lifecycle_source.Source).Value!, SemanticDocumentSet.Create([document], catalog));
        _model = result.Value!.Model;
        _results = SemanticLifecycleRunner.RunAll(_model);
    }

    [Fact] void should_select_v36() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V36);
    [Fact] void should_bind_the_state_machine() => _model.Application.StateMachines.Single().Transitions.Length.ShouldEqual(4);
    [Fact] void should_bind_the_workflow() => _model.Application.Workflows.Single().Nodes.Length.ShouldEqual(18);
    [Fact] void should_run_every_specification() => _results.Length.ShouldEqual(7);
    [Fact] void should_pass_every_specification() => string.Join('\n', _results.Where(result => !result.Passed).Select(result => $"{result.Owner}.{result.Name}: {string.Join("; ", result.Failures)}")).ShouldEqual(string.Empty);
    [Fact] void should_round_trip_the_model() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_model))).ShouldEqual(SemanticModelSerializer.Serialize(_model));
}
