// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_types_are_distinct_concepts : given.a_query
{
    void Establish() => Compile("query Get => R optional\n  by id OtherId", declarations: "concept OtherId : Uuid\n");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_not_equate_concepts_by_their_primitive() => Findings.Length.ShouldEqual(1);
}
