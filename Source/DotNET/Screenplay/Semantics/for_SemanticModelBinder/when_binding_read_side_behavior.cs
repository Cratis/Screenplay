// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_read_side_behavior : given.a_semantic_binder
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
                from ProjectRegistered key projectId
                  name = name
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_the_read_model_identifier() => ReadModel.Properties.Single(_ => _.Name == "projectId").IsIdentifier.ShouldBeTrue();
    [Fact] void should_bind_the_optional_query() => Query.Cardinality.ShouldEqual(SemanticQueryCardinality.ZeroOrOne);
    [Fact] void should_bind_snapshot_delivery() => Query.Delivery.ShouldEqual(SemanticQueryDelivery.Snapshot);
    [Fact] void should_bind_the_query_argument() => Query.Argument.Name.ShouldEqual("projectId");
    [Fact] void should_bind_the_affected_instance_key() => ((SemanticResolvedExpression)Projection.Transitions.Single().AffectedInstance.Key).Target.ShouldEqual(Event.Properties.Single(_ => _.Name == "projectId").Id);
    [Fact] void should_materialize_automap_and_explicit_mappings() => Projection.Transitions.Single().Mappings.Length.ShouldEqual(2);
    [Fact] void should_map_every_read_side_declaration_to_source() => _result.Value!.SourceMap.Entries.Length.ShouldEqual(16);

    SemanticEventContract Event => ChangeSlice.Events.Single();
    SemanticSlice ChangeSlice => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single(_ => _.Kind == SemanticSliceKind.StateChange);
    SemanticProjection Projection => ViewSlice.Projections.Single();
    SemanticKeyedQuery Query => ViewSlice.Queries.Single();
    SemanticReadModel ReadModel => ViewSlice.ReadModels.Single();
    SemanticSlice ViewSlice => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single(_ => _.Kind == SemanticSliceKind.StateView);
}
