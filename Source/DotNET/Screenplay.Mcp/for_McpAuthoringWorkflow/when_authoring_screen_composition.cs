// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_authoring_screen_composition : given.an_authoring_connection
{
    const string Application = """
        exposure for Shell
          property "shell:header".title label "Header title"
          property "shell:header".actions operations add, reorder fields label

        instance Browse
          set "shell:header".title = "Invoices"
          items "shell:header".actions
            item "export:csv"
              label = "Export"

        module Sales
          screen template Shell
            outlet detail
            display "Sales shell"
            scopes slice
            header contributes Actions
            body

            content header
              component scene.web.Header shellHeader
                id "shell:header"

            arrangement flow
              grid gap 8 columns 2 rows 1 grow
                header span 2
                body grow 2

          feature Invoices
            slice StateView Browse
              command Register
                invoiceId String

              screen Browse
                template Shell
                navigate to Browse
                  outlet detail
                contribute to Actions order 10
                  action Register
        """;

    JsonElement _opened;
    JsonElement _created;
    JsonElement _applied;
    JsonElement _broken;
    string _documentId = string.Empty;

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        _opened = Open();
        _created = Result("propose-source", Arguments(_opened, new { operation = "create-document", path = "application.play", stableKey = "application", source = Application }));
        Apply(_opened, _created);
        _applied = Open();
        _documentId = Page("documents", _applied.GetProperty("revision").GetString())
            .EnumerateArray().Single(document => document.GetProperty("path").GetString() == "application.play").GetProperty("documentId").GetString()!;
        var broken = File.ReadAllText(Path.Combine(RootPath, "application.play")).Replace("    outlet detail\n    display", "    display", StringComparison.Ordinal);
        _broken = Result("propose-source", Arguments(_applied, new { operation = "replace-document", documentId = _documentId, source = broken }));
    }

    [Fact] void should_accept_the_composition() => _created.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_round_trip_the_exposure() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("property \"shell:header\".actions operations add, reorder fields label");
    [Fact] void should_round_trip_the_instance() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("item \"export:csv\"");
    [Fact] void should_round_trip_template_content() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("content header");
    [Fact] void should_round_trip_the_grid() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("grid gap 8 columns 2 rows 1 grow");
    [Fact] void should_round_trip_the_screen_contribution() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("contribute to Actions order 10");
    [Fact] void should_read_the_exposure_ast() => Node("ExposedPropertySyntax").GetProperty("component").GetString().ShouldEqual("shell:header");
    [Fact] void should_read_the_instance_ast() => Node("ContributedItemSyntax").GetProperty("id").GetString().ShouldEqual("export:csv");
    [Fact] void should_read_the_screen_contribution_ast() => Node("ScreenContributionSyntax").GetProperty("order").GetInt32().ShouldEqual(10);
    [Fact] void should_report_a_navigation_to_a_removed_outlet() => _broken.GetRawText().ShouldContain("PLAY0630");
    [Fact] void should_refuse_to_propose_a_composition_with_a_missing_outlet() => _broken.GetProperty("success").GetBoolean().ShouldBeFalse();

    JsonElement Node(string kind) => Result("read-ast", new { expectedRevision = _applied.GetProperty("revision").GetString(), documentId = _documentId, kind, includeContent = true })
        .GetProperty("page").GetProperty("items").EnumerateArray().First().GetProperty("node");

    JsonElement Result(string tool, object arguments) => Call(tool, arguments).GetProperty("result").GetProperty("structuredContent");

    static object Arguments(JsonElement opened, object document) => new
    {
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        formatting = "CanonicalizeTouchedDocuments",
        documents = new[] { document }
    };
}
