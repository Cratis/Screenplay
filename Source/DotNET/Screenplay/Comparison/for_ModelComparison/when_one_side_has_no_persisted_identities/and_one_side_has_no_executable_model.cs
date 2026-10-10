// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_one_side_has_no_persisted_identities;

public class and_one_side_has_no_executable_model : given.two_models
{
    void Establish()
    {
        ChangeSource("system Store\n" + Source);
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithoutIdentities(_after));

    [Fact] void should_report_declaration_level_fallback() => _result.Sections.Single(section => section.Section == ComparisonSectionKind.Declarations).Gaps.Any(gap => gap.Kind == ComparisonGapKind.DeclarationLevelOnly).ShouldBeTrue();
    [Fact] void should_not_claim_executable_candidate() => _result.AfterExecutable.ShouldBeFalse();
}
