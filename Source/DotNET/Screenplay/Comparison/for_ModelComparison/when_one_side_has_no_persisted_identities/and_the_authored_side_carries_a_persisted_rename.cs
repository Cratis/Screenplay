// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_one_side_has_no_persisted_identities;

public class and_the_authored_side_carries_a_persisted_rename : given.two_models
{
    void Establish()
    {
        RenameMember();
        _before = _after;
        _after = Create(Source);
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithoutIdentities(_after));

    [Fact] void should_match_addresses_not_derived_identities() => _result.Declarations.Where(change => change.Declaration.Kind == "Property").Select(change => change.Change).ShouldContainOnly(DeclarationChangeKind.Removed, DeclarationChangeKind.Added);
    [Fact] void should_not_report_identity_migration() => _result.Identities.ShouldBeEmpty();
}
