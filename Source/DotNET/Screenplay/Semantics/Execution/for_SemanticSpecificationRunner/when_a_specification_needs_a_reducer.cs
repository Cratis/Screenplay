// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_a_specification_needs_a_reducer : Specification
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
              specification DepositChangesBalance
                when Deposit
                  accountId = "00000000-0000-0000-0000-000000000101"
                then AmountDeposited
                  accountId = "00000000-0000-0000-0000-000000000101"
                then readmodel Balance
                  accountId = "00000000-0000-0000-0000-000000000101"
            slice StateView BalanceView
              readmodel Balance
                accountId AccountId
              query BalanceById => Balance?
                by accountId AccountId
              reducer BalanceReducer => Balance
                on AmountDeposited
                  file Reducers/Deposited.cs
        """;

    SemanticExecutionPlanCompilation _result;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Accounts"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("accounts"), "accounts", "Accounts.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Accounts", SemanticDocumentSet.Create([document], catalog));
        _result = SemanticExecutionPlan.Compile(compilation.Value!.Model);
    }

    [Fact] void should_refuse_to_create_a_reference_plan() => _result.Plan.ShouldBeNull();
    [Fact] void should_require_a_target_to_compute_the_state() => _result.Issues.Single().Kind.ShouldEqual(SemanticPlanIssueKind.RequiresTargetReducer);
    [Fact] void should_name_the_read_model_needing_a_target() => _result.Issues.Single().Details.ShouldContain("requires a target provider");
}
