// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_checking_reducer_identity;

public class with_unknown_provenance : given.a_reducer_identity_model
{
    const string Inherited = "\n    slice Automation Follow\n      reaction Follow\n        when Recorded\n          produces Recorded";
    const string Capture = "\n    slice Translate Import\n      capture Import\n        source rows\n        key resource\n        append Recorded";

    [Fact] void should_skip_an_inherited_reaction_destination() => Warnings(Model + Inherited).ShouldBeEmpty();
    [Fact] void should_skip_a_reaction_cycle() => Warnings(Model + Inherited + Inherited.Replace("Follow", "Again", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_skip_captures_even_with_a_declared_command_type() => Warnings(Model + Capture).ShouldBeEmpty();
    [Fact] void should_skip_capture_fallback_without_a_command() => Warnings(Model[..Model.IndexOf("    slice StateChange", StringComparison.Ordinal)] + Capture).ShouldBeEmpty();
    [Fact] void should_skip_handler_outputs() => Warnings(Model + "\n    slice StateChange Code\n      command Code\n        handler\n          file Code.cs").ShouldBeEmpty();
    [Fact] void should_skip_reaction_attachments() => Warnings(Model + "\n    slice Automation Code\n      reaction Code\n        when Recorded\n          file Code.cs").ShouldBeEmpty();
    [Theory]
    [InlineData("ResourceId optional")]
    [InlineData("ResourceId[]")]
    [InlineData("Missing")]
    void should_skip_unresolved_or_non_scalar_destinations(string type) => Warnings(Model.Replace("resource ResourceId identifier", $"resource {type} identifier", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_skip_ambiguous_nominal_declarations() => Warnings("concept ResourceId : Uuid\n" + Model).ShouldBeEmpty();
    [Fact] void should_not_use_primitive_backing_for_ambiguous_concept_type_collisions() => Warnings("type ResourceId\n  value String\n" + Model).ShouldBeEmpty();
    [Fact] void should_skip_foreign_event_origins() => Warnings(Model.Replace("event Recorded", "public event Recorded from \"Partner\"", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_not_infer_provenance_from_fixtures() => Warnings(Model[..Model.IndexOf("    slice StateChange", StringComparison.Ordinal)] + "\n      specification Example\n        given Recorded for \"resource\"\n        when append Recorded for \"resource\"").ShouldBeEmpty();
    [Fact] void should_suppress_multi_domain_warning_with_any_unknown_producer() => Warnings(Model + SecondCommand + Inherited).ShouldBeEmpty();
    [Fact] void should_still_warn_for_a_different_complete_event() => Warnings(Model.Replace("on Recorded", "on Recorded\n        on Unknown", StringComparison.Ordinal)
        .Replace("event Recorded", "event Unknown\n      event Recorded", StringComparison.Ordinal)).Length.ShouldEqual(1);
}
