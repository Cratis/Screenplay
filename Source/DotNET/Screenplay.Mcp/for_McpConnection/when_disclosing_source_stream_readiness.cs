// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_disclosing_source_stream_readiness
{
    const string Sources = "eventsource Account\n  stream Transactions\n  stream Onboarding\n";
    const string Commands = "module Banking\n  feature Accounts\n    slice StateChange Deposit\n      command Deposit\n        stream Account.Transactions\n        produces event Deposited\n      command Import\n        stream Account.Transactions\n        handler\n          file Import.cs\n      command Begin\n        stream Account.Onboarding\n      command Plain\n        stream String\n      specification Routed\n        when Import\n      specification Ordinary\n        when Plain\n";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_disclose_routes_in_details_inventories_and_actions_in_either_file_order(bool reverse)
    {
        var documents = new[] { Document("sources.play", Sources), Document("commands.play", Commands) };
        var snapshot = new McpSnapshot([.. reverse ? documents.Reverse() : documents]);
        snapshot.Compilation.Success.ShouldBeTrue();
        foreach (var (address, kind) in new[]
        {
            ("Banking.Accounts.Deposit.Deposit", "Command"), ("Banking.Accounts.Deposit.Import", "Command"),
            ("Banking.Accounts.Deposit.Begin", "Command"), ("Banking.Accounts.Deposit.Routed", "Specification"),
            ("Banking.Accounts.Deposit", "Slice"), ("Account", "EventSource"), ("Account.Transactions", "EventStream")
        })
        {
            var details = Details(snapshot, address, kind, "summary");
            details.GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
            details.GetProperty("executionReadiness").GetString().ShouldContain("ESM v10 (PLAY0268)");
            var declaration = snapshot.Index.Find(address, kind).Single();
            Json(declaration.Details!).GetRawText().ShouldContain("ESM v10");
        }
        var commands = Details(snapshot, "Banking.Accounts.Deposit", "Slice", "commands").GetProperty("items");
        foreach (var command in commands.EnumerateArray().Where(command => command.GetProperty("name").GetString() != "Plain"))
        {
            command.GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
            command.GetProperty("executionReadiness").GetString().ShouldContain("ESM v10");
        }
        var specifications = Details(snapshot, "Banking.Accounts.Deposit", "Slice", "specifications").GetProperty("items");
        specifications[0].GetProperty("executionReadiness").GetString().ShouldContain("ESM v10");
        specifications[1].GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
        Details(snapshot, "Banking.Accounts.Deposit.Plain", "Command", "summary").GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
        snapshot.Index.Readiness.ModelSyntaxOnly.ShouldBeTrue();
        snapshot.Index.Readiness.ModelExecutionReadiness.ShouldContain("ESM v10");
        snapshot.Index.Find("Account.Transactions", "EventStream").Single().Scope.SequenceEqual(["Account"]).ShouldBeTrue();
        snapshot.Index.Find("Account.Transactions", "Operation").ShouldBeEmpty();
    }

    [Fact]
    void should_disclose_v10_for_the_actual_source_stream_conformance_fixture()
    {
        var root = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!Directory.Exists(Path.Combine(root.FullName, "Source", "Screenplay", "Compiler", "Conformance"))) root = root.Parent!;
        var source = File.ReadAllText(Path.Combine(root.FullName, "Source", "Screenplay", "Compiler", "Conformance", "source-streams.play"));
        var snapshot = new McpSnapshot([Document("model.play", source)]);
        snapshot.Compilation.Success.ShouldBeTrue();
        foreach (var name in new[] { "Deposit", "Import", "Begin" })
        {
            var details = Details(snapshot, $"Banking.Accounts.Deposit.{name}", "Command", "summary");
            details.GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
            details.GetProperty("executionReadiness").GetString().ShouldContain("ESM v10");
        }
        Details(snapshot, "Banking.Accounts.Deposit.LegacyNames", "Command", "summary").GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_separate_global_source_declarations_from_plain_member_capabilities(bool reverse)
    {
        var documents = new[] { Document("sources.play", Sources), Document("commands.play", "module M\n  feature F\n    slice StateChange S\n      command Plain\n        name String\n      specification T\n        when Plain\n") };
        var snapshot = new McpSnapshot([.. reverse ? documents.Reverse() : documents]);
        var model = Json(McpModelQueries.Describe(snapshot, 2, Json(new { view = "summary" })));
        model.GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
        model.GetProperty("executionReadiness").GetString().ShouldContain("ESM v10");
        foreach (var (address, kind) in new[] { ("M.F.S.Plain", "Command"), ("M.F.S.T", "Specification"), ("M.F.S", "Slice") })
        {
            var details = Details(snapshot, address, kind, "summary");
            details.GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
            details.GetProperty("executionReadiness").ValueKind.ShouldEqual(JsonValueKind.Null);
        }
    }

    [Fact]
    void should_prioritize_v10_over_operations_and_responses_without_changing_v8_or_v9()
    {
        const string source = "system Mailer\n" + Sources + "module M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n      command Routed\n        name String\n        returns name\n        stream Account.Onboarding\n        produces Send\n      command Operation\n        name String\n        returns name\n        produces Send\n      command Response\n        name String\n        returns name\n      specification T\n        when Routed\n";
        var snapshot = new McpSnapshot([Document("model.play", source)]);
        Details(snapshot, "M.F.S.Routed", "Command", "summary").GetProperty("executionReadiness").GetString().ShouldContain("ESM v10");
        Details(snapshot, "M.F.S.T", "Specification", "summary").GetProperty("executionReadiness").GetString().ShouldContain("ESM v10");
        Details(snapshot, "M.F.S.Operation", "Command", "summary").GetProperty("executionReadiness").GetString().ShouldContain("ESM v9");
        Details(snapshot, "M.F.S.Response", "Command", "response").GetProperty("executionReadiness").GetString().ShouldEqual("Unavailable until ESM v8 (PLAY0268); no response type is emitted.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_preserve_imported_physical_scope_and_block_ambiguous_routing(bool reverse)
    {
        var documents = new[]
        {
            Document("application.play", "import Account.Transactions\n" + Sources + "module M\n  feature F\n    import \"commands.play\"\n    import \"types.play\"\n"),
            Document("types.play", "description \"Imported\"\n  type Transactions\n    value String"),
            Document("commands.play", "slice StateChange S\n  command C\n    stream Account.Transactions\n      deeper String\n  specification T\n    when C\n")
        };
        var snapshot = new McpSnapshot([.. reverse ? documents.Reverse() : documents]);
        snapshot.Compilation.Success.ShouldBeFalse();
        snapshot.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0505").ShouldBeTrue();
        var command = (CommandSyntax)snapshot.Index.Find("M.F.S.C", "Command").Single().Syntax;
        command.StreamCandidates.Single().PropertyCandidate.ShouldNotBeNull();
        command.Properties.Select(property => property.Name).SequenceEqual(["deeper"]).ShouldBeTrue();
        Details(snapshot, "M.F.S.C", "Command", "summary").GetProperty("executionReadiness").GetString().ShouldContain("ESM v10");
        Details(snapshot, "M.F.S.T", "Specification", "summary").GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_find_routed_command_actions_across_native_feature_imports(bool reverse)
    {
        var documents = new[]
        {
            Document("application.play", Sources + "module M\n  feature F\n    import \"commands.play\"\n    import \"specifications.play\"\n"),
            Document("commands.play", "slice StateChange A\n  command C\n    stream Account.Onboarding\n    handler\n      file C.cs\n"),
            Document("specifications.play", "import M.F.A.C\nslice StateChange B\n  specification T\n    when C\n")
        };
        var snapshot = new McpSnapshot([.. reverse ? documents.Reverse() : documents]);
        snapshot.Compilation.Success.ShouldBeTrue();
        Details(snapshot, "M.F.B.T", "Specification", "summary").GetProperty("executionReadiness").GetString().ShouldContain("ESM v10");
        Details(snapshot, "M.F.A", "Slice", "commands").GetProperty("items")[0].GetProperty("executionReadiness").GetString().ShouldContain("ESM v10");
    }

    static JsonElement Details(McpSnapshot snapshot, string address, string kind, string view) => Json(McpDeclarationDetails.Read(snapshot, Json(new { address, kind, view }))).GetProperty("details");

    static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value, McpJson.Options);

    static WorkspaceDocument Document(string path, string source) => WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source));
}
