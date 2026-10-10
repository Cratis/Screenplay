// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_one_side_has_no_persisted_identities;

public class and_members_are_absent_on_one_side : given.two_models
{
    void Establish()
    {
        ChangeSource(Source + "\n      screen History\n");
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithoutIdentities(_after));

    [Fact] void should_use_null_for_absent_member_hash() => _result.Members.Any(change => change.Declaration.Kind == "Screen" && change.BeforeHash is null && change.AfterHash is not null).ShouldBeTrue();
}
