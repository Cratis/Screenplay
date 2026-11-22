// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionLoop.when_materializing_reaction_facts;

public class and_audit_identity_and_clock_are_missing : for_SemanticReactionSettlement.given.a_settlement
{
    SemanticUnsupported _failure;
    SemanticReactionLoop _loop;

    void Establish() => Prepare("reaction Continue\n  when Started\n    produces Continued");

    void Because()
    {
        var reaction = _plan.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        var auditRead = new SemanticEventContextExpression(SemanticEventContextValueKind.CausedBySubject, SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
        trigger = trigger with
        {
            Produces = [trigger.Produces.Single() with { Mappings = [new(_plan.Commands.Values.Single().Properties.Single().Id, auditRead)] }]
        };
        _loop = new(_evaluator, _plan, _world);
        _failure = (SemanticUnsupported)_loop.Fire(reaction, trigger, new Dictionary<SemanticId, SemanticValue>(), SemanticExpressionRootKind.Event, _input, null)!;
    }

    [Fact] void should_report_audit_identity_before_the_missing_clock() => _failure.Details.ShouldEqual("Reaction 'Continue' has no caller audit identity for $context.causedBy.");
    [Fact] void should_report_the_reaction_capability() => _failure.Capability.ShouldEqual(SemanticExecutionCapability.Reaction);
    [Fact] void should_retain_the_failure_world() => _failure.World.ShouldBeSame(_world);
    [Fact] void should_not_append_a_fact() => _loop.Facts.ShouldBeEmpty();
    [Fact] void should_retain_the_input_world() => _loop.World.ShouldBeSame(_world);
}
