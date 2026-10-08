// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_identity_is_unknown : given.a_query
{
    void Establish() => Compile("query Get => R optional\n  by missing Int", "key $eventSourceId");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_not_guess_a_nominal_identity_type() => Findings.ShouldBeEmpty();
}
