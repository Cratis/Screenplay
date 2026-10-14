// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_checking_reducer_identity;

public class with_scoped_producers : given.a_reducer_identity_model
{
    const string Other = "\nmodule Other\n  feature Reporting\n    slice StateChange Record\n      event Recorded\n      command Record\n        selection SelectionId identifier\n        produces Recorded\n          for selection";

    [Fact] void should_not_mix_duplicate_short_event_names() => Warnings(Model + Other).Single().Message.StartsWith("Query 'ByMonth'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_resolve_fully_qualified_cross_module_productions() => Warnings(Model + Other.Replace("produces Recorded", "produces Work.Reporting.Totals.Recorded", StringComparison.Ordinal)).Single().Message.StartsWith("Reducer 'Fold'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_resolve_nested_features() => Warnings(string.Join('\n', Model.Split('\n').Select(line => line switch { "  feature Reporting" => "  feature Outer\n    feature Reporting", _ when line.StartsWith("    ", StringComparison.Ordinal) => "  " + line, _ => line }))).Length.ShouldEqual(1);
    [Fact] void should_not_depend_on_first_producer_order() => Warnings(Model + SecondCommand).Single().Message.ShouldEqual(Warnings(Model.Replace("    slice StateChange Record", SecondCommand.TrimStart('\n') + "\n    slice StateChange Record", StringComparison.Ordinal)).Single().Message);
    [Fact] void should_skip_an_ambiguous_observed_event() => Warnings(Model.Replace("      event Recorded\n        resource ResourceId\n", string.Empty, StringComparison.Ordinal)
        + "\n    slice StateChange First\n      event Recorded\n\n    slice StateChange Second\n      event Recorded\n").ShouldBeEmpty();
}
