// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_summarizing_slices;

public class when_an_event_changes : given.a_model_with_dependent_slices
{
    void Establish() => ChangeSource(Source.Replace("event Registered\n        name ProjectName", "event Registered\n        name ProjectName\n        code String", StringComparison.Ordinal));

    void Because() => Compare();

    [Fact] void should_report_only_the_owning_slice() => _result.Slices.Slices.Select(slice => slice.Address).ShouldContainOnly("Projects.Registration.Register");
    [Fact] void should_report_the_slice_as_changed() => Slice("Projects.Registration.Register").Changes.ShouldContainOnly(SliceChangeKind.Changed);
    [Fact] void should_keep_the_slice_address() => Slice("Projects.Registration.Register").BeforeAddress.ShouldEqual("Projects.Registration.Register");
    [Fact] void should_list_the_changed_event() => Slice("Projects.Registration.Register").Declarations.ShouldContain(declaration => declaration.Kind == "Event" && declaration.AfterAddress == "Projects.Registration.Register.Registered");
    [Fact] void should_list_the_projecting_slice_as_dependent() => Slice("Projects.Registration.Register").DependentSlices.ShouldContain("Projects.Registration.Listing");
    [Fact] void should_not_list_an_unrelated_slice_as_dependent() => Slice("Projects.Registration.Register").DependentSlices.ShouldNotContain("Projects.Registration.Archive");
    [Fact] void should_report_the_projecting_slice_as_affected() => _result.Slices.AffectedSlices.ShouldContain("Projects.Registration.Listing");
}
