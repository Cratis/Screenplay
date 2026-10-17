// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_a_collection_by_matches_a_composite_key_part : given.a_query
{
    void Establish() => Compile("query List => R[]\n  by sourceId Uuid optional", "key Identity\n  sourceId = sourceId", "type Identity\n  sourceId Uuid\n");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_accept_the_key_part() => Findings.ShouldBeEmpty();
}
