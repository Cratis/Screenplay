// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_a_scenario_needs_an_opaque_reducer : given.a_model
{
    JsonElement _report;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
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
              query BalanceById => Balance optional
                by accountId AccountId
              reducer BalanceReducer => Balance
                on AmountDeposited
                  file Reducers/Deposited.cs
        """);

    void Because() => _report = Call("run-specifications").GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_not_report_a_pass() => _report.GetProperty("outcome").GetString().ShouldEqual("unsupported");
    [Fact] void should_still_run_the_unrelated_scenario() => _report.GetProperty("passed").GetInt32().ShouldEqual(1);
    [Fact] void should_not_count_unsupported_as_executed() => _report.GetProperty("executed").GetInt32().ShouldEqual(1);
    [Fact] void should_name_the_blocking_capability() => _report.GetProperty("page").GetProperty("items")[0].GetProperty("capability").GetString().ShouldEqual("Projection");
    [Fact] void should_explain_the_opaque_reducer() => _report.GetProperty("page").GetProperty("items")[0].GetProperty("reason").GetString()!.ShouldContain("BalanceReducer");
}
