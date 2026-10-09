// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_querying_specification_cases : given.a_connection
{
    JsonElement _cases;
    JsonElement _fixtures;
    McpSnapshot _snapshot;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        module M
          feature F
            slice StateChange S
              command Record
                id String identifier
                amount Int
                produces Recorded
                  for id
                  amount = amount
              event Recorded
                amount Int
              specification Recording
                parameter amount Int
                case Small amount = 10
                case Large amount = 100
                when Record amount = case.amount
                  id = "record"
                then Recorded amount = case.amount
        """);

    void Because()
    {
        _snapshot = new(Root.Read());
        _cases = JsonSerializer.SerializeToElement(McpDeclarationDetails.Read(_snapshot, JsonSerializer.SerializeToElement(new { address = "M.F.S.Recording", kind = "Specification", view = "cases" })), McpJson.Options).GetProperty("details").GetProperty("items");
        _fixtures = JsonSerializer.SerializeToElement(McpFixtureQueries.Values(_snapshot, "M.F.S.Recording", null, null, null, @case: "Small"), McpJson.Options).GetProperty("page").GetProperty("items");
    }

    [Fact] void should_page_every_case() => _cases.GetArrayLength().ShouldEqual(2);
    [Fact] void should_expose_derived_addresses() => _cases[0].GetProperty("effectiveAddress").GetString().ShouldEqual("M.F.S.Recording_Small");
    [Fact] void should_resolve_a_derived_name_to_its_table() => _snapshot.Index.Find("M.F.S.Recording_Small", "Specification").Single().Name.ShouldEqual("Recording");
    [Fact] void should_expose_the_selected_case() => _snapshot.Index.Find("M.F.S.Recording_Small", "Specification").Single().Case.ShouldEqual("Small");
    [Fact] void should_filter_case_fixtures() => _fixtures.GetArrayLength().ShouldEqual(3);
    [Fact] void should_report_parameter_origins() => _fixtures[0].GetProperty("caseParameter").GetString().ShouldEqual("amount");
    [Fact] void should_report_case_origins() => _fixtures[0].GetProperty("origin").GetString().ShouldEqual("case");
    [Fact] void should_link_case_references() => _snapshot.Index.Incoming(_snapshot.Index.Find("M.F.S.Recording.amount", "SpecificationParameter").Single()).Count().ShouldEqual(2);
    [Fact] void should_select_every_case_by_table_scope() => McpSpecificationExecution.Run(Workspace(), scope: "M.F.S.Recording").Passed.ShouldEqual(2);
    [Fact] void should_select_one_derived_case() => McpSpecificationExecution.Run(Workspace(), "M.F.S.Recording_Small").Passed.ShouldEqual(1);
    [Fact] void should_select_every_case_by_table_filter() => McpSpecificationExecution.Run(Workspace(), "M.F.S.Recording").Passed.ShouldEqual(2);
}
