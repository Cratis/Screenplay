// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_comparing_the_documented_sources : Specification
{
    ComparedModel _before = null!;
    ComparedModel _after = null!;
    ModelDifference _difference = null!;

    void Establish()
    {
        const string source = "module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        name String\n";
        _before = ComparedModel.FromSources("Projects",
            new Dictionary<string, string> { ["application.play"] = source });
        _after = ComparedModel.FromSources("Projects",
            new Dictionary<string, string> { ["application.play"] = source + "        extra String\n" });
    }

    void Because() => _difference = ModelComparison.Compare(_before, _after);

    [Fact] void should_report_the_documented_addition() => _difference.Events.Single(change => change.Change == EventContractChangeKind.PropertyAdded).Property.ShouldEqual("extra");
    [Fact] void should_keep_the_contract_breaking_flag() => _difference.Events.Single(change => change.Change == EventContractChangeKind.PropertyAdded).ContractBreaking.ShouldBeTrue();
    [Fact] void should_match_by_address() => _difference.Matching.ShouldEqual(DeclarationMatching.Address);
}
