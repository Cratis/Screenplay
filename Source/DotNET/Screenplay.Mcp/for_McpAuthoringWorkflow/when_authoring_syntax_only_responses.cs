// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_authoring_syntax_only_responses : given.an_authoring_connection
{
    const string ResponseSource = "concept ProjectId : Uuid\nmodule Projects\n  feature Registration\n    slice StateChange Register\n      command RegisterProject\n        projectId ProjectId generated identifier // keep identity\n        receiptId ProjectId generated\n        name String\n        produces event ProjectRegistered // keep event\n          name String = name\n        returns // keep response\n          result = receiptId // keep field\n      specification Registered\n        when RegisterProject\n          for \"11111111-1111-1111-1111-111111111111\"\n          generated receiptId = \"22222222-2222-2222-2222-222222222222\"\n          name = \"Apollo\"\n        then returns\n          result = \"22222222-2222-2222-2222-222222222222\"\n";

    [Fact]
    void should_discover_syntax_details_sources_and_fixtures_without_response_identities()
    {
        var opened = Start(ResponseSource);
        var details = Result("declaration-details", new { address = "Projects.Registration.Register.RegisterProject", kind = "Command", view = "response" }).GetProperty("details");
        details.GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
        details.GetProperty("executionReadiness").GetString().ShouldContain("Not admitted by any supported executable model (ESM) version yet");
        details.GetProperty("fields")[0].GetProperty("source").GetString().ShouldEqual("receiptId");
        details.GetProperty("fields")[0].GetProperty("inferredType").GetProperty("name").GetString().ShouldEqual("ProjectId");
        var propertyPage = Result("declaration-details", new { address = "Projects.Registration.Register.RegisterProject", kind = "Command", view = "properties" });
        propertyPage.GetRawText().ShouldContain("\"isGenerated\":true");
        var field = Node("ResponseFieldSyntax", opened.GetProperty("revision").GetString()!);
        field.GetProperty("semanticId").ValueKind.ShouldEqual(JsonValueKind.Null);
        var values = Result("find-fixtures", new { limit = 50 }).GetRawText();
        values.ShouldContain("generatedValues");
        values.ShouldContain("thenReturns");
        values.ShouldContain("ProjectId");
        Result("syntax-schema", new { kind = "CommandSyntax" }).GetRawText().ShouldContain("response");
        Result("syntax-schema", new { kind = "ResponseFieldSyntax" }).GetRawText().ShouldContain("source");
    }

    [Fact]
    void should_add_replace_remove_response_and_fields_only_after_acceptance()
    {
        const string source = Source + "\n";
        var opened = Start(source);
        var command = Node("CommandSyntax", opened.GetProperty("revision").GetString()!);
        var response = new { kind = "RecordCommandResponseSyntax", fields = new[] { Field("result", "name") } };
        var proposal = Propose(opened, new { operation = "add", parent = command.GetProperty("handle"), member = "response", node = response });
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
        Candidate(proposal).Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0268").ShouldBeTrue();
        opened = Apply(opened, proposal).GetProperty("workspace");
        var field = Node("ResponseFieldSyntax", opened.GetProperty("revision").GetString()!);
        proposal = Propose(opened, new { operation = "replace", target = field.GetProperty("handle"), node = Field("label", "name") });
        opened = Apply(opened, proposal).GetProperty("workspace");
        var record = Node("RecordCommandResponseSyntax", opened.GetProperty("revision").GetString()!);
        proposal = Propose(opened, new { operation = "add", parent = record.GetProperty("handle"), member = "fields", node = Field("id", "projectId") });
        opened = Apply(opened, proposal).GetProperty("workspace");
        var fields = Result("read-ast", new { expectedRevision = opened.GetProperty("revision").GetString(), kind = "ResponseFieldSyntax", includeContent = true }).GetProperty("page").GetProperty("items");
        var id = fields.EnumerateArray().Single(field => field.GetProperty("node").GetProperty("name").GetString() == "id");
        proposal = Propose(opened, new { operation = "remove", target = id.GetProperty("handle") });
        opened = Apply(opened, proposal).GetProperty("workspace");
        record = Node("RecordCommandResponseSyntax", opened.GetProperty("revision").GetString()!);
        proposal = Propose(opened, new { operation = "replace", target = record.GetProperty("handle"), node = new { kind = "ScalarCommandResponseSyntax", source = new { kind = "PropertyResponseSourceSyntax", property = "name" } } });
        opened = Apply(opened, proposal).GetProperty("workspace");
        var scalar = Node("ScalarCommandResponseSyntax", opened.GetProperty("revision").GetString()!);
        proposal = Propose(opened, new { operation = "remove", target = scalar.GetProperty("handle") });
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
        new ScreenplayCompiler().Compile(File.ReadAllText(Path.Combine(RootPath, "application.play"))).Value.Modules.Single().Features.Single().Slices.Single().Commands.Single().Response.ShouldBeNull();
    }

    [Fact]
    void should_refuse_executable_validation_and_stale_response_handles_without_writes()
    {
        var opened = Start(ResponseSource);
        var field = Node("ResponseFieldSyntax", opened.GetProperty("revision").GetString()!);
        var operation = new { operation = "replace", target = field.GetProperty("handle"), node = Field("result", "projectId") };
        var refused = Call("propose-ast", Arguments(opened, operation, "Executable"));
        refused.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        refused.GetRawText().ShouldContain("PLAY0268");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(ResponseSource);
        var proposal = Propose(opened, operation);
        opened = Apply(opened, proposal).GetProperty("workspace");
        var before = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        var stale = Call("propose-ast", Arguments(opened, operation));
        stale.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(before);
    }

    [Fact]
    void should_refuse_unverifiable_extraction_and_preserve_contract_during_rename()
    {
        var opened = Start(ResponseSource);
        var @event = Node("EventSyntax", opened.GetProperty("revision").GetString()!);
        var extraction = Call("propose-extract-inline-event", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            subject = @event.GetProperty("handle"),
            formatting = "CanonicalizeTouchedDocuments",
            validation = "Authoring"
        });
        extraction.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        extraction.GetRawText().ShouldContain("canonical executable bytes");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(ResponseSource);
        var rename = Result("propose-rename", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            target = @event.GetProperty("handle"), expectedName = "ProjectRegistered", newName = "ProjectCreated",
            formatting = "PreserveTrivia", validation = "Authoring"
        });
        var candidate = Candidate(rename);
        candidate.Documents.Single().Text.ShouldContain("// keep response");
        candidate.Documents.Single().Text.ShouldContain("// keep field");
        WorkspaceSyntaxIndex.Create(candidate).Entries.Count(entry => entry.Node is ResponseFieldSyntax).ShouldEqual(1);
        candidate.Documents.Single().Text.ShouldContain("id \"ProjectRegistered\"");
        candidate.Documents.Single().Text.ShouldContain("receiptId ProjectId generated");
        candidate.Documents.Single().Text.ShouldContain("result = receiptId");
        Apply(opened, rename).GetProperty("success").GetBoolean().ShouldBeTrue();
        Open().GetProperty("revision").GetString().ShouldNotBeNull();
    }

    [Fact]
    void should_validate_the_documentation_fixture_as_syntax_but_refuse_execution()
    {
        var repository = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "AGENTS.md")))
        {
            repository = repository.Parent;
        }

        var source = File.ReadAllText(Path.Combine(repository!.FullName, "Documentation", "screenplay", "fixtures", "generated-responses.play"));
        new ScreenplayCompiler().Compile(source).Success.ShouldBeTrue();
        var opened = Start(source);
        var diagnostics = Page("executable-diagnostics", opened.GetProperty("revision").GetString()!).GetRawText();
        diagnostics.ShouldContain("PLAY0268");
    }

    JsonElement Start(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
        return Open();
    }

    JsonElement Propose(JsonElement opened, object operation) => Result("propose-ast", Arguments(opened, operation));

    static object Arguments(JsonElement opened, object operation, string validation = "Authoring") => new
    {
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        formatting = "CanonicalizeTouchedDocuments", validation, operations = new[] { operation }
    };

    static object Field(string name, string source) => new { kind = "ResponseFieldSyntax", name, type = (object?)null, source = new { kind = "PropertyResponseSourceSyntax", property = source } };
}
