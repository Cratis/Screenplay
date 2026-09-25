// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_variants;

// Chronicle VariantReclassifier.cs:28-72 turns global handlers into root joins and sibling entries into removals.
public class affected_variant_instances : a_work_item
{
    [Fact] void should_report_the_variant_join_as_a_root_join_with_identifier_property_and_no_key()
    {
        var model = Bind(Source).Value!.Model;
        var projection = model.Application.Modules.Single().Features.Single().Slices.SelectMany(_ => _.Projections)
            .Single(_ => _.Name.EndsWith(":BacklogItem", StringComparison.Ordinal));
        var join = projection.GetAffectedInstances().Single(_ => _.Block == SemanticAffectedProjectionBlock.Join);
        join.Path.ShouldBeEmpty();
        join.Match.ShouldEqual(SemanticAffectedProjectionMatch.ManyByPropertyAndEventSource);
        join.Key.ShouldBeNull();
        var readModel = model.Application.Modules.Single().Features.Single().Slices.SelectMany(_ => _.ReadModels)
            .Single(_ => _.Name == "BacklogItem");
        join.Property.ShouldEqual(readModel.Properties.Single(_ => _.IsIdentifier).Id);
    }

    [Fact] void should_report_a_sibling_entering_event_as_a_removal()
    {
        var model = Bind(Source).Value!.Model;
        var slice = model.Application.Modules.Single().Features.Single().Slices;
        var projection = slice.SelectMany(_ => _.Projections).Single(_ => _.Name.EndsWith(":BacklogItem", StringComparison.Ordinal));
        var enteringEvent = slice.SelectMany(_ => _.Events).Single(_ => _.Name == "IssueStarted");
        projection.GetAffectedInstances().Single(_ => _.Block == SemanticAffectedProjectionBlock.Removal)
            .EventContract.ShouldEqual(enteringEvent.Id);
    }
}
