// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_required_reads : given.a_protected_read_model
{
    CompilationResult<SemanticCompilation> _result;
    void Because() => _result = Bind(Source);
    [Fact] void should_bind() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_select_v24() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V24);
    [Fact] void should_select_the_protected_mode() => _result.Value!.Model.Application.CommandReadMode.ShouldEqual(SemanticCommandReadMode.Protected);
    [Fact] void should_bind_the_lookup_key() => Command(_result.Value!.Model).Reads.Single().Key.ShouldEqual(Command(_result.Value.Model).Properties.Single(property => property.Name == "id").Id);
    [Fact] void should_bind_the_view_identity() => Command(_result.Value!.Model).Reads.Single().ReadModel.ShouldEqual(View(_result.Value.Model).Id);
    [Fact] void should_require_presence() => Command(_result.Value!.Model).Reads.Single().Presence.ShouldEqual(SemanticDecisionReadPresence.Required);
    [Fact] void should_bind_a_read_path_mapping() => Command(_result.Value!.Model).Produces.Single().Mappings.Single().Source.ShouldBeOfExactType<SemanticReadPathExpression>();
    [Fact] void should_bind_a_read_path_requirement() => ((SemanticComparison)Command(_result.Value!.Model).Requirements.Single().Condition).Left.Read.ShouldEqual("View");
    [Fact] void should_round_trip_the_model() => SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Value!.Model)).Revision.ShouldEqual(_result.Value.Model.Revision);
    [Fact] void should_not_execute_unguarded() => SemanticExecutionPlan.Compile(_result.Value!.Model).Issues.ShouldContain(issue => issue.Kind == SemanticPlanIssueKind.UnsupportedDecisionReads);
}
