// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.given;

public class a_settlement : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    protected ISemanticEvaluator _evaluator;
    protected SemanticReactionSettlement _settlement;
    protected SemanticFact _input;
    protected SemanticWorld _world;
    protected SemanticExecutionResult _result;
    protected readonly DateTimeOffset _occurred = new(2026, 10, 2, 9, 0, 0, TimeSpan.Zero);

    void Establish()
    {
        var evaluator = new SemanticEvaluator();
        _evaluator = Substitute.For<ISemanticEvaluator>();
        _evaluator.Execute(Arg.Any<SemanticExecutionPlan>(), Arg.Any<SemanticWorld>(), Arg.Any<SemanticExecutionRequest>())
            .Returns(call => evaluator.Execute(call.Arg<SemanticExecutionPlan>(), call.Arg<SemanticWorld>(), call.Arg<SemanticExecutionRequest>()));
        _settlement = new(_evaluator);
    }

    protected void Prepare(string reactions)
    {
        var reactionSource = string.Join(Environment.NewLine, reactions.Split('\n').Select(line => $"      {line}"));
        Compile(
            $$"""
            policy Access
              require role "Automation"
            module M
              feature F
                slice Automation Work
                  event Started
                  event Continued
                  event Completed
                  command Complete
                    item String identifier
                    authorize Access
                    produces Completed
                      for item
            {{reactionSource}}
            """);
        _input = new(EventId("Started"), SemanticValue.Text("one"), [])
        {
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("one")))
        };
        _world = SemanticWorld.Create([_input], []);
    }

    protected SemanticId EventId(string name) => _plan.Events.Values.Single(value => value.Name == name).Id;
}
