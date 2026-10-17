// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_a_single_result_by_is_required : given.a_query
{
    void Establish() => Compile("query Get => R optional\n  by id Uuid");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_accept_the_identity_type() => Findings.ShouldBeEmpty();
}
