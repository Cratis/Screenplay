// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_a_projection_key_determines_identity : given.a_query
{
    void Establish() => Compile("query Get => R optional\n  by missing Int");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_use_the_key_source_type() => Findings.Length.ShouldEqual(1);
}
