// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_projection_affected_key;

// The from-block key drives the transition; the projection-level key beside it is never consulted.
public class with_a_from_block_key_and_a_projection_level_key : given.a_semantic_binder
{
    const string Source =
        """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name ProjectName
              query ProjectById => ProjectSummary?
                by projectId ProjectId
              projection ProjectSummaryProjection => ProjectSummary
                key name
                from ProjectRegistered
                  key projectId
                  name = name
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_key_the_transition_on_the_from_block_key() => AffectedKey.Target.ShouldEqual(EventProperty("projectId"));

    SemanticResolvedExpression AffectedKey => (SemanticResolvedExpression)Slices.Single(_ => _.Kind == SemanticSliceKind.StateView).Projections.Single().Transitions.Single().AffectedInstance.Key;
    SemanticId EventProperty(string name) => Slices.Single(_ => _.Kind == SemanticSliceKind.StateChange).Events.Single().Properties.Single(_ => _.Name == name).Id;
    IEnumerable<SemanticSlice> Slices => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices;
}
