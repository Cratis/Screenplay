// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_a_filter_has_an_incompatible_type : given.a_query
{
    void Establish() => Compile("query Get => R[]\n  filter name Int");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_report_the_type() => Findings.Single().Message.ShouldContain("'Int'");
}
