// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_a_filter_is_optional : given.a_query
{
    void Establish() => Compile("query Get => R[]\n  filter name String optional");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_accept_an_optional_filter_on_a_required_field() => Findings.ShouldBeEmpty();
}
