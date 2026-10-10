// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_one_side_has_no_persisted_identities;

public class and_generations_share_display_addresses : given.two_models
{
    void Establish()
    {
        const string source = EventSource + "      event Registered generation 2\n        name String\n";
        _before = Create(source);
        _after = Create(source.Replace("generation 2\n        name String", "generation 2\n        name Int", StringComparison.Ordinal));
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithoutIdentities(_after));

    [Fact] void should_keep_generation_in_the_matching_key() => _result.Members.Count(change => change.Declaration.Kind == "Property" && change.Member == "type").ShouldEqual(1);
    [Fact] void should_not_confuse_the_generations_with_presence_changes() => _result.Declarations.ShouldBeEmpty();
}
