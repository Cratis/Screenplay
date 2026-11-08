// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_summarizing_slices;

public class when_nothing_changes : given.a_model_with_dependent_slices
{
    void Because() => Compare();

    [Fact] void should_report_no_slices() => _result.Slices.Slices.ShouldBeEmpty();
    [Fact] void should_report_no_model_wide_changes() => _result.Slices.ModelWide.ShouldBeEmpty();
    [Fact] void should_report_no_affected_slices() => _result.Slices.AffectedSlices.ShouldBeEmpty();
}
