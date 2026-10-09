// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_routed_persona_case_tables : given.a_semantic_binder
{
    const string Declarations = """
        eventsource Account
          identifier String
          stream Ledger
            streamId String
        policy Member
          require authenticated and role "A"
        persona Accountant
          policy Member
        module M
          feature F
            slice StateChange S
              event Recorded
                amount Int
              command Record
                id String identifier
                period String
                amount Int
                stream Account.Ledger
                  streamId = period
                authorize Member
                produces Recorded
                  for id
                  amount = amount
        """;
    const string Table = """
              specification Recording
                parameter period String
                parameter amount Int
                case Small period = "2026-10"
                  amount = 10
                case Large period = "2026-11"
                  amount = 100
                given caller as Accountant
                given Recorded amount = 1
                  for "record"
                  stream Account.Ledger
                    streamId = case.period
                when Record amount = case.amount
                  id = "record"
                  period = case.period
                then Recorded amount = case.amount
                  for "record"
                  stream Account.Ledger
                    streamId = case.period
        """;
    const string Scenario = """
              specification Recording_NAME
                given caller
                  authenticated
                  role "A"
                given Recorded amount = 1
                  for "record"
                  stream Account.Ledger
                    streamId = "PERIOD"
                when Record amount = AMOUNT
                  id = "record"
                  period = "PERIOD"
                then Recorded amount = AMOUNT
                  for "record"
                  stream Account.Ledger
                    streamId = "PERIOD"
        """;
    CompilationResult<SemanticCompilation> _table;
    CompilationResult<SemanticCompilation> _explicit;

    void Because()
    {
        _table = Bind(Declarations + "\n" + Table);
        _explicit = Bind(Declarations + "\n" + ExpandScenario("Small", "2026-10", "10") + "\n" + ExpandScenario("Large", "2026-11", "100"));
    }

    [Fact] void should_bind_the_routed_persona_table() => Assert.True(_table.Success, string.Join('\n', _table.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_bind_the_hand_written_scenarios() => _explicit.Success.ShouldBeTrue();
    [Fact] void should_use_the_event_routes_version() => _table.Value.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
    [Fact] void should_match_hand_written_bytes() => SemanticModelSerializer.Serialize(_table.Value.Model).SequenceEqual(SemanticModelSerializer.Serialize(_explicit.Value.Model)).ShouldBeTrue();
    [Fact] void should_match_hand_written_revision() => _table.Value.Model.Revision.ShouldEqual(_explicit.Value.Model.Revision);
    [Fact] void should_keep_case_origins() => _table.Value.SpecificationOrigins.Values.Select(origin => origin.Case.Name).ShouldContainOnly("Small", "Large");
    [Fact] void should_keep_persona_origins() => _table.Value.SpecificationOrigins.Values.SelectMany(origin => origin.Steps).Where(step => step.Role == "given caller").SelectMany(step => step.Values).All(value => value.Origin == SpecificationValueOrigin.Persona && value.Persona == "Accountant" && value.Policy == "Member").ShouldBeTrue();

    [Fact]
    void should_execute_each_case_with_its_history_and_produced_route()
    {
        foreach (var origin in _table.Value.SpecificationOrigins)
        {
            var run = new SemanticSpecificationRunner().Run(_table.Value, origin.Key);
            Assert.True(run.Passed, string.Join(';', run.Failures));
            var expected = origin.Value.Case.Name == "Small" ? "2026-10" : "2026-11";
            run.Execution.World.Facts.Length.ShouldEqual(2);
            run.Execution.World.Facts.All(fact => fact.Route == new SemanticEventRoute("Account", "Ledger", expected)).ShouldBeTrue();
        }
    }

    [Fact]
    void should_expand_composite_case_keys_in_declaration_order()
    {
        var declarations = "concept AmountId : Int\n" + Declarations.Replace("    streamId String", "    streamId\n      period String\n      amount AmountId", StringComparison.Ordinal)
            .Replace("amount Int", "amount AmountId", StringComparison.Ordinal)
            .Replace("          streamId = period", "          streamId\n            amount = amount\n            period = period", StringComparison.Ordinal);
        var tableSource = Table.Replace("            streamId = case.period", "            streamId\n              amount = case.amount\n              period = case.period", StringComparison.Ordinal);
        var scenario = Scenario.Replace("            streamId = \"PERIOD\"", "            streamId\n              amount = AMOUNT\n              period = \"PERIOD\"", StringComparison.Ordinal);
        var table = Bind(declarations + "\n" + tableSource);
        var explicitCases = Bind(declarations + "\n" + ExpandScenario("Small", "2026-10", "10", scenario) + "\n" + ExpandScenario("Large", "2026-11", "100", scenario));
        Assert.True(table.Success, string.Join('\n', table.Diagnostics.Select(diagnostic => diagnostic.Message)));
        explicitCases.Success.ShouldBeTrue();
        SemanticModelSerializer.Serialize(table.Value.Model).SequenceEqual(SemanticModelSerializer.Serialize(explicitCases.Value.Model)).ShouldBeTrue();
        table.Value.SpecificationOrigins.Keys.All(id => new SemanticSpecificationRunner().Run(table.Value, id).Passed).ShouldBeTrue();
    }

    [Fact]
    void should_type_routed_case_fixtures_by_an_unambiguous_producer_when_the_source_has_no_identifier()
    {
        var table = Bind(Declarations.Replace("  identifier String\n", string.Empty, StringComparison.Ordinal) + "\n" + Table);
        Assert.True(table.Success, string.Join('\n', table.Diagnostics.Select(diagnostic => diagnostic.Message)));
        table.Value.SpecificationOrigins.Keys.All(id => new SemanticSpecificationRunner().Run(table.Value, id).Passed).ShouldBeTrue();
    }

    static string ExpandScenario(string name, string period, string amount, string source = Scenario) => source.Replace("NAME", name, StringComparison.Ordinal).Replace("PERIOD", period, StringComparison.Ordinal).Replace("AMOUNT", amount, StringComparison.Ordinal);
}
