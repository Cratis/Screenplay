// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing.for_AuthoringIndex.given;

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_a_projection_publishes_a_public_event;

public class when_indexing_event_targets : given.two_models
{
    void Establish()
    {
        var source = "module Merchandising\n  feature Publication\n" + string.Join('\n', event_target_applications.Catalog.Select(value => $"    slice Translate {value.Slice}\n      direction outbound\n      projection {value.Projection} => {value.Event}\n        from Input\n      public event {value.Event}\n        value String\n      event Input\n        value String"));
        _before = Create(source, application: "Contracts");
        _after = Create(source.Replace("value String", "value String\n        extra String", StringComparison.Ordinal), application: "Contracts");
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithoutIdentities(_before), ComparedModel.WithoutIdentities(_after));

    [Fact] void should_find_each_projection_as_a_direct_builds_dependant_of_the_changed_event() => _result.Dependants.Where(change => change.Role == "builds" && change.Changed.Kind == "Event").Select(change => change.DependantAddress).Distinct().Order(StringComparer.Ordinal).ShouldEqual(event_target_applications.Catalog.Select(value => "Merchandising.Publication." + value.Slice + "." + value.Projection).Order(StringComparer.Ordinal));
    [Fact] void should_not_invent_uncomparable_views() => _result.Sections.SelectMany(section => section.Gaps).Any(gap => gap.Kind == ComparisonGapKind.NotComparableIndexed).ShouldBeFalse();
}
