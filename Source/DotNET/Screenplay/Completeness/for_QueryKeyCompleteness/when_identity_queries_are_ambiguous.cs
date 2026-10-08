// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_identity_queries_are_ambiguous : given.a_query
{
    void Establish() => Compile("query Get => R optional\n  by id Int", "key $eventSourceId", extraQueries: "query Other => R optional\n  by name String");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_not_choose_a_keyed_query() => Findings.ShouldBeEmpty();
}
