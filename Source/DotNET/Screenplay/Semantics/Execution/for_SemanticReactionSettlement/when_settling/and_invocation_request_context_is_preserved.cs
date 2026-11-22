// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.Extensions;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_invocation_request_context_is_preserved : given.a_settlement
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_keep_context_and_consume_pins_in_invocation_order(bool hasOccurrence)
    {
        CompileReaction();
        var requests = new List<SemanticExecutionRequest>();
        _evaluator.Configure().Execute(Arg.Any<SemanticExecutionPlan>(), Arg.Any<SemanticWorld>(), Arg.Any<SemanticExecutionRequest>())
            .Returns(call =>
            {
                requests.Add(call.Arg<SemanticExecutionRequest>());
                return new SemanticAccepted(call.Arg<SemanticWorld>(), [], []);
            });
        var command = _plan.Commands.Values.Single();
        var generated = command.Properties.Single(property => property.IsGenerated);
        var first = SemanticValue.Text("11111111-1111-4111-8111-111111111111");
        var second = SemanticValue.Text("22222222-2222-4222-8222-222222222222");
        var loop = new SemanticReactionLoop(_evaluator, _plan, SemanticWorld.Empty) { GivenTimeZone = "Europe/Oslo" };
        loop.Pin([new(command.Id, generated.Id, first), new(command.Id, generated.Id, second)]);
        var reaction = _plan.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        trigger = trigger with { Invokes = [trigger.Invokes[0] with { OnRefused = [new("any", null, [])] }, trigger.Invokes[1]] };
        loop.Fire(reaction, trigger, new Dictionary<SemanticId, SemanticValue>(), SemanticExpressionRootKind.Event, null, hasOccurrence ? _occurred : null).ShouldBeNull();
        requests.Count.ShouldEqual(2);
        loop.Invocations.Select(invocation => invocation.Command).ShouldEqual([command.Id, command.Id]);
        requests.Select(request => request.CollectRefusalDetails).ShouldEqual([true, false]);
        requests.Select(request => request.Values[0].Value).ShouldEqual([SemanticValue.Text("one"), SemanticValue.Text("two")]);
        requests.Select(request => request.GeneratedValues.Single().Value).ShouldEqual([first, second]);
        foreach (var request in requests)
        {
            request.Command.ShouldEqual(command.Id);
            request.Queries.ShouldBeEmpty();
            request.AllocatedIdentities.ShouldBeEmpty();
            request.Values.Select(value => value.TargetProperty).ShouldEqual(command.Properties.Where(property => !property.IsGenerated).Select(property => property.Id));
            request.Values[1].Value.ShouldEqual(SemanticValue.Null);
            request.GeneratedValues.Single().TargetProperty.ShouldEqual(generated.Id);
            request.ContextTimeZone.ShouldEqual("Europe/Oslo");
            request.ScenarioTimeZone.ShouldEqual("Europe/Oslo");
            request.ContextTimeZones.Single().ShouldEqual(new KeyValuePair<string, string>("billingZone", "Europe/Oslo"));
            request.ReactionOrigin.ShouldEqual(reaction.Id);
            request.RunsAs.ShouldBeSame(reaction.RunsAs);
            request.Caller.ShouldBeNull();
            if (hasOccurrence)
            {
                request.Occurrence.ShouldEqual(new SemanticCommandOccurrence(_occurred, string.Empty, string.Empty, string.Empty) { IsTimeOnly = true });
            }
            else
            {
                request.Occurrence.ShouldBeNull();
            }
        }
    }

    void CompileReaction() => Compile(
        """
        concept CompletionId : Uuid
        context
          billingZone time zone
        module M
          feature F
            slice Automation Work
              event Started
              command Complete
                id CompletionId generated identifier
                item String
                note String?
                calendar from $context.billingZone
              reaction Continue
                runs as system role "Automation"
                when Started
                  invokes Complete
                    item = "one"
                  invokes Complete
                    item = "two"
        """);
}
