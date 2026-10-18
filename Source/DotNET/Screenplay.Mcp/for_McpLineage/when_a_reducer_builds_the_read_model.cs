// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_a_reducer_builds_the_read_model : given.a_lineage_model
{
    JsonElement _report;
    JsonElement[] _hops = [];
    JsonElement _code;

    void Establish() => Write("""
        concept AccountId : Uuid
        module Accounts
          feature Balances
            slice StateChange Deposits
              command Deposit
                accountId AccountId identifier
                amount Int
                produces AmountDeposited
                  for accountId
                  amount = amount
              event AmountDeposited
                amount Int
            slice StateView BalanceView
              readmodel Balance
                accountId AccountId
                total Int
              query BalanceById => Balance optional
                by accountId AccountId
              reducer BalanceReducer => Balance
                on AmountDeposited
                  file Reducers/Deposited.cs
        """);

    void Because()
    {
        _report = Trace("Accounts.Balances.BalanceView.Balance.total");
        _hops = Hops(_report);
        _code = _hops.Single(hop => hop.GetProperty("role").GetString() == "code");
    }

    [Fact] void should_report_the_reducer_as_opaque() => _code.GetProperty("status").GetString().ShouldEqual("opaque");
    [Fact] void should_name_the_reducer() => Text(_code, "address").ShouldEqual("Accounts.Balances.BalanceView.BalanceReducer");
    [Fact] void should_name_the_reducer_kind() => Text(_code, "kind").ShouldEqual("Reducer");
    [Fact] void should_not_guess_an_event_property_behind_the_code() => _hops.Any(hop => hop.GetProperty("role").GetString() == "event-property").ShouldBeFalse();
    [Fact] void should_not_be_complete() => _report.GetProperty("complete").GetBoolean().ShouldBeFalse();
}
