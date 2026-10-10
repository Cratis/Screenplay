// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_one_side_has_no_persisted_identities;

public class and_declarations_share_addresses : given.two_models
{
    void Establish()
    {
        ChangeSource(Source.Replace("readmodel Projects\n        name String", "readmodel Projects\n        name Int", StringComparison.Ordinal));
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithoutIdentities(_after));

    [Fact] void should_match_by_address() => _result.Matching.ShouldEqual(DeclarationMatching.Address);
    [Fact] void should_compare_property_members() => _result.Members.Any(change => change.Declaration.Kind == "Property" && change.Member == "type").ShouldBeTrue();
    [Fact] void should_not_report_identity_changes() => _result.Identities.ShouldBeEmpty();
    [Fact] void should_not_expose_synthetic_identities() => _result.Members.All(change => change.Declaration.SemanticId is null).ShouldBeTrue();
    [Fact] void should_report_identity_coverage_gap() => _result.Sections.Single(section => section.Section == ComparisonSectionKind.Identities).Gaps.Single().Kind.ShouldEqual(ComparisonGapKind.IdentitiesNotCompared);
}
