// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_extending_a_world;

public class with_facts_reaching_a_reducer : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    SemanticWorld _world;
    SemanticExecutionResult _result;

    void Establish()
    {
        Compile("""
            module Accounts
              feature Balances
                slice StateChange Deposits
                  command Deposit
                    accountId Uuid identifier
                    produces AmountDeposited
                      for accountId
                  event AmountDeposited
                slice StateView BalanceView
                  readmodel Balance
                    accountId Uuid
                  query BalanceById => Balance?
                    by accountId Uuid
                  reducer BalanceReducer => Balance
                    on AmountDeposited
                      file Reducers/Deposited.cs
            """);
        _world = SemanticWorld.Empty;
    }

    void Because()
    {
        var deposited = _plan.Events.Values.Single();
        var key = SemanticValue.Text("00000000-0000-0000-0000-000000000101");
        var type = _plan.Commands.Values.Single().Properties.Single().Type;
        _result = new SemanticEvaluator().ExtendWorld(_plan, _world, [new(deposited.Id, key, []) { Context = new(new(type, key)) }]);
    }

    [Fact] void should_be_unsupported_for_projection() => ((SemanticUnsupported)_result).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
    [Fact] void should_name_the_reducer() => ((SemanticUnsupported)_result).Details.ShouldContain("BalanceReducer");
    [Fact] void should_leave_the_input_world_unchanged() => _result.World.ShouldBeSame(_world);
}
