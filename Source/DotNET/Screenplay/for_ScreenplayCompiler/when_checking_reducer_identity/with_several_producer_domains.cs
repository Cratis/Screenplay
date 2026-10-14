// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_checking_reducer_identity;

public class with_several_producer_domains : given.a_reducer_identity_model
{
    Diagnostic[] _warnings;

    void Because() => _warnings = Warnings(Model + SecondCommand + SecondCommand.Replace("SelectionId", "AllocationId", StringComparison.Ordinal).Replace("Select", "Allocate", StringComparison.Ordinal));

    [Fact] void should_report_only_the_reducer_warning() => _warnings.Length.ShouldEqual(1);
    [Fact] void should_locate_the_reducer() => _warnings[0].Location.Line.ShouldEqual(12);
    [Fact] void should_sort_event_domain_pairs() => _warnings[0].Message.ShouldContain("Recorded/AllocationId, Recorded/ResourceId, Recorded/SelectionId");
    [Fact] void should_not_repeat_the_query_warning() => _warnings[0].Message.StartsWith("Reducer 'Fold'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_warn_even_with_only_a_collection_query() => Warnings((Model + SecondCommand).Replace("ByMonth => Totals", "ByMonth => Totals[]", StringComparison.Ordinal)).Length.ShouldEqual(1);
}
