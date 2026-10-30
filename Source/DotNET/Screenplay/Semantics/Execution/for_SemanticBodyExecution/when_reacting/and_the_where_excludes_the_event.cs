// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_the_where_excludes_the_event : given.a_reaction_effect
{
    void Establish() => Configure(Source.Replace("      specification", "        where name == \"Other\"\n      specification", StringComparison.Ordinal));
    void Because() => Fire();
    [Fact] void should_accept_the_excluded_event() => Failure.ShouldBeNull();
    [Fact] void should_not_call_the_provider() => Provider.Calls.ShouldEqual(0);
    [Fact] void should_not_append_facts() => Loop.Facts.ShouldBeEmpty();
}
