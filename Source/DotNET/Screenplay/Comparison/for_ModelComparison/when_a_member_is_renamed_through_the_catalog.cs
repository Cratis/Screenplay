// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_a_member_is_renamed_through_the_catalog : given.two_models
{
    void Establish()
    {
        RenameMember();
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_a_rename() => _result.Declarations.Any(change => change.Change == DeclarationChangeKind.Renamed && change.Declaration.AfterAddress!.EndsWith(".title", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_identity_migration() => _result.Identities.Any(change => change.Change == IdentityChangeKind.Migrated && change.Declaration.AfterAddress!.EndsWith(".title", StringComparison.Ordinal)).ShouldBeTrue();
}
