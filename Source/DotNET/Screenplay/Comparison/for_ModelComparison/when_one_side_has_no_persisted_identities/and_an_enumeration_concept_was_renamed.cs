// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_one_side_has_no_persisted_identities;

public class and_an_enumeration_concept_was_renamed : when_enumeration_members_change.given.an_enumeration
{
    void Establish() => ChangeSource(Model("paid\n  issued\n  draft").Replace("Status", "InvoiceStatus", StringComparison.Ordinal));
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithoutIdentities(_after));

    [Fact] void should_report_concept_removal_and_addition() => _result.Declarations.Where(change => change.Declaration.BeforeAddress == "Status" || change.Declaration.AfterAddress == "InvoiceStatus").Select(change => change.Change).ShouldContainOnly(DeclarationChangeKind.Removed, DeclarationChangeKind.Added);
    [Fact] void should_not_compare_members_across_an_inferred_rename() => _result.Enumerations.ShouldBeEmpty();
    [Fact] void should_use_address_matching() => _result.Matching.ShouldEqual(DeclarationMatching.Address);
}
