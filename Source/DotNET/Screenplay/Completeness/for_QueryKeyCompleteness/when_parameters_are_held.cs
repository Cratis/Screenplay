// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_parameters_are_held : given.a_query
{
    void Establish() => Compile("query Get => R optional\n  by id Uuid\n  filter name String");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_accept_the_parameters() => Findings.ShouldBeEmpty();
}
