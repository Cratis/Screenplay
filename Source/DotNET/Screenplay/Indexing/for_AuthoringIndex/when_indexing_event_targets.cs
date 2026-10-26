// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Indexing.for_AuthoringIndex;

public class when_indexing_event_targets : Specification
{
    AuthoringIndex _index = null!;

    void Because() => _index = new AuthoringSnapshot(given.event_target_applications.CatalogSources()).Index;

    [Fact] void should_request_events_for_all_three_catalog_targets() => _index.References.Where(reference => reference.Role == "builds").Select(reference => string.Join(',', reference.Kinds)).ShouldEqual(["Event", "Event", "Event"]);
    [Fact] void should_resolve_the_explicit_events_declared_in_the_later_document() => _index.ResolvedReferences.Where(edge => edge.Reference.Role == "builds").Select(edge => string.Join(',', edge.Candidates.Select(candidate => candidate.Kind + ":" + candidate.Address))).ShouldEqual(given.event_target_applications.Catalog.Select(value => "Event:Merchandising.Publication." + value.Slice + "." + value.Event));
    [Fact] void should_not_invent_readmodels() => _index.Declarations.Where(declaration => declaration.Kind == "ReadModel").ShouldBeEmpty();
    [Fact] void should_preserve_authored_target_names() => _index.References.Where(reference => reference.Role == "builds").Select(reference => reference.Name).ShouldEqual(given.event_target_applications.Catalog.Select(value => value.Event));
    [Fact] void should_preserve_projection_ownership() => _index.References.Where(reference => reference.Role == "builds").Select(reference => reference.Owner!.Address).ShouldEqual(given.event_target_applications.Catalog.Select(value => "Merchandising.Publication." + value.Slice + "." + value.Projection));
    [Fact] void should_preserve_target_source_locations() => _index.References.Where(reference => reference.Role == "builds").Select(reference => $"{reference.Location.Path}:{reference.Location.Line}:{reference.Location.Column}").ShouldEqual(["a-targets.play:5:7", "a-targets.play:11:7", "a-targets.play:17:7"]);
    [Fact] void should_preserve_event_input_resolution() => _index.ResolvedReferences.Where(edge => edge.Reference.Role == "from").Select(edge => string.Join(',', edge.Candidates.Select(candidate => candidate.Kind + ":" + candidate.Address))).ShouldEqual(given.event_target_applications.Catalog.Select(value => "Event:Merchandising.Publication." + value.Slice + ".Input"));
}
