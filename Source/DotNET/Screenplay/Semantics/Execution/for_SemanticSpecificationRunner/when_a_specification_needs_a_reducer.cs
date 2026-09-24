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
              specification DepositOnlyAppends
                when Deposit
                  accountId = "00000000-0000-0000-0000-000000000101"
                then AmountDeposited
                  accountId = "00000000-0000-0000-0000-000000000101"
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
    SemanticSpecificationRun _dependent = null!;
    SemanticSpecificationRun _unrelated = null!;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Accounts"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("accounts"), "accounts", "Accounts.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Accounts", SemanticDocumentSet.Create([document], catalog));
        _result = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        var runner = new SemanticSpecificationRunner();
        var specifications = _result.Plan!.Specifications.Values;
        _dependent = runner.Run(_result.Plan, specifications.Single(value => value.Name == "DepositChangesBalance").Id);
        _unrelated = runner.Run(_result.Plan, specifications.Single(value => value.Name == "DepositOnlyAppends").Id);
    }

    [Fact] void should_compile_the_reference_plan() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_dependent_specification_as_unsupported() => (_dependent.Execution is SemanticUnsupported).ShouldBeTrue();
    [Fact] void should_name_the_reducer() => ((SemanticUnsupported)_dependent.Execution).Details.ShouldContain("BalanceReducer");
    [Fact] void should_not_count_the_unsupported_specification_as_passed() => _dependent.Passed.ShouldBeFalse();
    [Fact] void should_run_an_unrelated_specification() => _unrelated.Passed.ShouldBeTrue();
}
