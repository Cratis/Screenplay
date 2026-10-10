// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_one_side_has_no_persisted_identities;

public class and_the_source_is_incomplete : given.two_models
{
    void Establish()
    {
        ChangeSource(Source + "    slice\n");
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithoutIdentities(_after));

    [Fact] void should_report_incomplete_source() => _result.Sections.SelectMany(section => section.Gaps).Any(gap => gap.Kind == ComparisonGapKind.IncompleteSource).ShouldBeTrue();
    [Fact] void should_not_claim_completeness() => _result.Complete.ShouldBeFalse();
}
