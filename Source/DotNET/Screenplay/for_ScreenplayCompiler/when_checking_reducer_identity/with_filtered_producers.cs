// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_checking_reducer_identity;

public class with_filtered_producers : given.a_reducer_identity_model
{
    const string Sources = "eventsource Resource\n  identifier ResourceId\n  stream Daily\n    streamId Month\n  stream Other\neventsource Selection\n  identifier SelectionId\n  stream Daily\n";
    static string Routed => Sources + Model.Replace("on Recorded", "from Resource.Daily\n        on Recorded", StringComparison.Ordinal)
        .Replace("produces Recorded", "stream Resource.Daily\n          streamId = \"October\"\n        produces Recorded", StringComparison.Ordinal)
        + SecondCommand.Replace("produces Recorded", "stream Selection.Daily\n        produces Recorded", StringComparison.Ordinal);

    [Fact] void should_ignore_source_and_stream_rename_pins_as_nominal_domains() => Warnings(Routed.Replace("eventsource Resource\n", "eventsource Resource\n  id \"OldResource\"\n", StringComparison.Ordinal).Replace("  stream Daily\n", "  stream Daily\n    id \"OldDaily\"\n", StringComparison.Ordinal)).Length.ShouldEqual(1);
    [Fact] void should_filter_before_comparing_domains() => Warnings(Routed).Single().Message.StartsWith("Query 'ByMonth'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_use_stream_id_type_as_instance_identity() => Warnings(Routed).Single().Message.ShouldContain("identity ResourceId");
    [Fact] void should_accept_one_domain_across_distinct_streams() => Warnings(Routed.Replace("from Resource.Daily\n        ", string.Empty, StringComparison.Ordinal).Replace("identifier SelectionId", "identifier ResourceId", StringComparison.Ordinal).Replace("selection SelectionId identifier", "selection ResourceId identifier", StringComparison.Ordinal).Replace("Month key", "ResourceId key", StringComparison.Ordinal).Replace("by month Month", "by month ResourceId", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_use_a_production_override_instead_of_the_command_route() => Warnings(Routed.Replace("for resource", "for resource\n          stream Resource.Other", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_keep_filter_exclusion_without_inventing_identity_evidence()
    {
        var diagnostics = new ScreenplayCompiler().Compile(Routed.Replace("from Resource.Daily", "from Resource.Other", StringComparison.Ordinal)).Diagnostics;
        diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ObserverFilterExcludesEveryProducer).ShouldBeTrue();
        diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.ReducerIdentityMismatch).ShouldBeEmpty();
    }
    [Fact] void should_not_discard_unknown_routing_of_a_capture() => Warnings(Routed + "\n    slice Translate Import\n      capture Import\n        source rows\n        key resource\n        append Recorded").ShouldBeEmpty();
}
