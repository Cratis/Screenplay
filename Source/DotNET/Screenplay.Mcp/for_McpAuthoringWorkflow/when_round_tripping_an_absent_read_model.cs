// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.given;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_round_tripping_an_absent_read_model : an_authoring_connection
{
    JsonElement _schema;
    JsonElement _node;
    JsonElement _references;
    JsonElement _proposal;
    JsonElement _details;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"),
            "module Projects\n  feature Registration\n    slice StateView Lookup\n      readmodel ItemView\n        id String\n      query ItemById => ItemView?\n        by id String\n      specification NoItem\n        then no readmodel ItemView for \"missing\"\n");
        Initialize();
    }

    void Because()
    {
        _schema = Result("syntax-schema", new { kind = "SpecificationAbsentReadModelSyntax" });
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        _node = Node("SpecificationAbsentReadModelSyntax", revision!);
        _references = Result("find-references", new { address = "Projects.Registration.Lookup.ItemView", kind = "ReadModel" });
        _details = Result("declaration-details", new { address = "Projects.Registration.Lookup", kind = "Slice", view = "specifications" });
        var target = Node("ReadModelSyntax", revision!).GetProperty("handle");
        _proposal = Call("propose-rename", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            target,
            expectedName = "ItemView",
            newName = "RenamedView"
        }).GetProperty("result");
    }

    [Fact] void should_discover_the_closed_absence_schema() => _schema.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
    [Fact] void should_round_trip_the_typed_key() => _node.GetProperty("node").GetProperty("key").GetProperty("value").GetString().ShouldEqual("missing");
    [Fact] void should_find_the_absent_view_reference() => _references.GetProperty("references").EnumerateArray().Any(item => item.GetRawText().Contains("thenAbsentReadModel", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_the_absence_as_an_assertion() => _details.GetProperty("details").GetProperty("items")[0].GetProperty("thenAbsentReadModels").GetInt32().ShouldEqual(1);
    [Fact] void should_propose_the_rename() => _proposal.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_return_an_accepted_proposal() => _proposal.GetProperty("structuredContent").GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_disk_unchanged_until_apply() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("then no readmodel ItemView");
}
