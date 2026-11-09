// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Projections.for_ProjectionAutoMap;

// A dictionary-entry target writes into its base property: the base is what is explicitly mapped, never the key's last segment.
public class when_a_mapping_targets_a_dictionary_entry : Specification
{
    (string Target, string Source)[] _result;

    void Because() => _result = [.. ProjectionAutoMap.Properties<string>(
        [
            new RemoveKeyMappingSyntax("slots.$eventContent.slot", new SourceLocation(1, 1)),
            new RemoveKeyMappingSyntax("byUser.$causedBy.userName", new SourceLocation(2, 1))
        ],
        ["slots", "slot", "byUser", "userName", "$eventContent"],
        ["slots", "slot", "byUser", "userName", "$eventContent"],
        name => name,
        (_, _) => true,
        isJoin: false)];

    [Fact] void should_not_automap_the_base_properties() => _result.Select(pair => pair.Target).ShouldNotContain("slots");
    [Fact] void should_not_automap_the_base_of_a_caused_by_key() => _result.Select(pair => pair.Target).ShouldNotContain("byUser");
    [Fact] void should_automap_properties_named_like_the_key() => _result.ShouldContainOnly(("slot", "slot"), ("userName", "userName"), ("$eventContent", "$eventContent"));
}
