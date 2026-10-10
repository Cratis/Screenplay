// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_expected_outcomes_change : given.two_models
{
    void Establish()
    {
        ChangeSource(Source.Replace("then Registered\n          name = \"First\"", "then Registered\n          name = \"Second\"", StringComparison.Ordinal));
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_compare_the_effective_outcome() => _result.Specifications.Any(change => change.Change == SpecificationChangeKind.ExpectedOutcomeChanged).ShouldBeTrue();
}
