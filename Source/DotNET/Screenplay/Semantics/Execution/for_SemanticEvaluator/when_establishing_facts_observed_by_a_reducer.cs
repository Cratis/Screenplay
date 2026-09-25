// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_establishing_facts_observed_by_a_reducer : Specification
{
    const string Source =
        """
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
        """;

    SemanticExecutionResult _result = null!;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Accounts"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("balances"), "balances", "Accounts.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Accounts", SemanticDocumentSet.Create([document], catalog));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var @event = plan.Events.Values.Single();
        _result = new SemanticEvaluator().EstablishWorld(plan, [new(@event.Id, SemanticValue.Text("00000000-0000-0000-0000-000000000101"), [])]);
    }

    [Fact] void should_fail_closed_on_the_opaque_transition()
    {
        _result.ShouldBeOfExactType<SemanticUnsupported>();
        ((SemanticUnsupported)_result).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
        ((SemanticUnsupported)_result).Details.ShouldContain("BalanceReducer");
        ReferenceEquals(_result.World, SemanticWorld.Empty).ShouldBeTrue();
    }
}
