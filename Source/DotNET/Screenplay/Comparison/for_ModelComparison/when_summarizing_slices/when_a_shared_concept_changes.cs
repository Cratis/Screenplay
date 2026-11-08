// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_summarizing_slices;

public class when_a_shared_concept_changes : given.a_model_with_dependent_slices
{
    void Establish() => ChangeSource(Source.Replace("concept ProjectName : String", "concept ProjectName : Int", StringComparison.Ordinal));

    void Because() => Compare();

    [Fact] void should_report_the_concept_as_model_wide() => _result.Slices.ModelWide.ShouldContain(declaration => declaration.Kind == "Concept" && declaration.AfterAddress == "ProjectName");
    [Fact] void should_not_attribute_it_to_a_slice() => _result.Slices.Slices.ShouldBeEmpty();
    [Fact] void should_report_the_slices_that_use_it_as_affected() => _result.Slices.AffectedSlices.ShouldContain("Projects.Registration.Register");
    [Fact] void should_not_report_a_slice_that_does_not_use_it() => _result.Slices.AffectedSlices.ShouldNotContain("Projects.Registration.Archive");
}
