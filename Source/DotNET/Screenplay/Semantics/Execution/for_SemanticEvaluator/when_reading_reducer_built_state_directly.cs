// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_reading_reducer_built_state_directly : Specification
{
    const string Source =
        """
        concept AccountId : Uuid
        module Accounts
          feature Balances
            slice StateChange Deposits
              command Deposit
                accountId AccountId identifier
                produces AmountDeposited
                  for accountId
                  accountId = accountId
              event AmountDeposited
                accountId AccountId
            slice StateView BalanceView
              readmodel Balance
                accountId AccountId
              query BalanceById => Balance?
                by accountId AccountId
              reducer BalanceReducer => Balance
                on AmountDeposited
                  file Reducers/Deposited.cs
        """;

    SemanticUnsupported _readOnly = null!;
    SemanticUnsupported _commandQuery = null!;
    SemanticUnsupported _existingState = null!;
    SemanticUnsupported _appendedState = null!;
    SemanticWorld _before = null!;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Accounts"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("accounts"), "accounts", "Accounts.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Accounts", SemanticDocumentSet.Create([document], catalog));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var query = plan.Queries.Values.Single();
        var command = plan.Commands.Values.Single();
        var key = SemanticValue.Text("00000000-0000-0000-0000-000000000101");
        var queries = new[] { new SemanticQueryRequest(query.Id, key) }.ToImmutableArray();
        var evaluator = new SemanticEvaluator();
        _before = SemanticWorld.Empty;
        _readOnly = (SemanticUnsupported)evaluator.Execute(plan, _before, SemanticExecutionRequest.ForQueries(queries));
        var request = SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, key)], queries);
        _commandQuery = (SemanticUnsupported)evaluator.Execute(plan, _before, request);
        var readModel = plan.ReadModels.Values.Single();
        var populated = SemanticWorld.Create([], [new(readModel.Id, key, [new(readModel.Properties.Single().Id, key)])]);
        _existingState = (SemanticUnsupported)evaluator.Execute(plan, populated, request with { Queries = [] });
        _appendedState = (SemanticUnsupported)SemanticEvaluator.Append(
            plan,
            populated,
            new(plan.Events.Values.Single().Id, key, []),
            [],
            null);
    }

    [Fact] void should_reject_a_direct_read_only_query() => _readOnly.Capability.ShouldEqual(SemanticExecutionCapability.Query);
    [Fact] void should_reject_a_direct_command_query() => _commandQuery.Capability.ShouldEqual(SemanticExecutionCapability.Query);
    [Fact] void should_name_the_reducer() => _readOnly.Details.ShouldContain("BalanceReducer");
    [Fact] void should_not_commit_a_queried_command() => ReferenceEquals(_commandQuery.World, _before).ShouldBeTrue();
    [Fact] void should_not_expose_existing_reducer_state_from_a_command() => _existingState.Capability.ShouldEqual(SemanticExecutionCapability.Projection);
    [Fact] void should_not_expose_existing_reducer_state_from_an_append() => _appendedState.Capability.ShouldEqual(SemanticExecutionCapability.Projection);
}
