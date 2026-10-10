// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_one_side_has_no_persisted_identities;

public class and_the_applications_differ : given.two_models
{
    void Establish()
    {
        _after = Create(Source, application: "Other");
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithoutIdentities(_after));

    [Fact] void should_match_the_same_addresses() => _result.Declarations.ShouldBeEmpty();
    [Fact] void should_compare_the_same_members() => _result.Members.ShouldBeEmpty();
    [Fact] void should_retain_equal_counts() => _result.AfterDeclarations.ShouldEqual(_result.BeforeDeclarations);
}
