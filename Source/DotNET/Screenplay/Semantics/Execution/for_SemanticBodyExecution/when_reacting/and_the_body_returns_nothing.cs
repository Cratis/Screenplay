// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_the_body_returns_nothing : given.a_reaction_effect
{
    void Establish() => Answer = SemanticReactionBodyResult.Empty;
    void Because() => Fire();
    [Fact] void should_accept_empty() => Failure.ShouldBeNull();
    [Fact] void should_not_append_facts() => Loop.Facts.ShouldBeEmpty();
    [Fact] void should_not_invoke_commands() => Loop.Invocations.ShouldBeEmpty();
}
