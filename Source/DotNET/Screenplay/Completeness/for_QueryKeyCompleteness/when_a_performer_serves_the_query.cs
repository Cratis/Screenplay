// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_a_performer_serves_the_query : given.a_query
{
    void Establish() => Compile("query Get => R[]\n  filter month Int\n  performer\n    file query.cs");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_skip_opaque_queries() => Findings.ShouldBeEmpty();
}
