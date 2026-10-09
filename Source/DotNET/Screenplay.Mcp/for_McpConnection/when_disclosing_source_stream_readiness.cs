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
        foreach (var (address, kind, feature) in new[]
        {
            ("Banking.Accounts.Deposit.Deposit", "Command", null), ("Banking.Accounts.Deposit.Import", "Command", "command handlers"),
            ("Banking.Accounts.Deposit.Begin", "Command", null), ("Banking.Accounts.Deposit.Routed", "Specification", "command handlers"),
            ("Banking.Accounts.Deposit", "Slice", "command handlers"), ("Account", "EventSource", null), ("Account.Transactions", "EventStream", null)
        })
        {
            Readiness(Details(snapshot, address, kind, "summary"), feature);
            var declaration = snapshot.Index.Find(address, kind).Single();
            var details = Json(declaration.Details!).GetRawText();
            details.ShouldNotContain("event sources, streams and routes (#302)");
            if (feature is not null) details.ShouldContain(feature);
        }
        var commands = Details(snapshot, "Banking.Accounts.Deposit", "Slice", "commands").GetProperty("items");
        foreach (var command in commands.EnumerateArray().Where(command => command.GetProperty("name").GetString() != "Plain"))
        {
            Readiness(command, command.GetProperty("name").GetString() == "Import" ? "command handlers" : null);
        }
        var specifications = Details(snapshot, "Banking.Accounts.Deposit", "Slice", "specifications").GetProperty("items");
        Readiness(specifications[0], "command handlers");
        specifications[1].GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
        Details(snapshot, "Banking.Accounts.Deposit.Plain", "Command", "summary").GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
        snapshot.Index.Readiness.ModelSyntaxOnly.ShouldBeTrue();
        snapshot.Index.Readiness.ModelExecutionReadiness.ShouldContain("command handlers");
        snapshot.Index.Readiness.ModelExecutionReadiness.ShouldNotContain("(#302)");
        snapshot.Index.Find("Account.Transactions", "EventStream").Single().Scope.SequenceEqual(["Account"]).ShouldBeTrue();
        snapshot.Index.Find("Account.Transactions", "Operation").ShouldBeEmpty();
    }

    [Fact]
    void should_disclose_admitted_routes_and_the_remaining_handler_for_the_actual_source_stream_conformance_fixture()
    {
        var root = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!Directory.Exists(Path.Combine(root.FullName, "Source", "Screenplay", "Compiler", "Conformance"))) root = root.Parent!;
        var source = File.ReadAllText(Path.Combine(root.FullName, "Source", "Screenplay", "Compiler", "Conformance", "source-streams.play"));
        var snapshot = new McpSnapshot([Document("model.play", source)]);
        snapshot.Compilation.Success.ShouldBeTrue();
        foreach (var name in new[] { "Deposit", "Import", "Begin" })
        {
            Readiness(Details(snapshot, $"Banking.Accounts.Deposit.{name}", "Command", "summary"), name == "Import" ? "command handlers" : null);
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
        Readiness(model);
        foreach (var (address, kind) in new[] { ("M.F.S.Plain", "Command"), ("M.F.S.T", "Specification"), ("M.F.S", "Slice") })
        {
            var details = Details(snapshot, address, kind, "summary");
            details.GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
            details.GetProperty("executionReadiness").ValueKind.ShouldEqual(JsonValueKind.Null);
        }
    }

    [Fact]
    void should_list_remaining_unadmitted_features_when_routes_operations_and_responses_coexist()
    {
        const string source = "system Mailer\n" + Sources + "module M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n      command Routed\n        name String\n        returns name\n        stream Account.Onboarding\n        produces Send\n      command Operation\n        name String\n        returns name\n        produces Send\n      command Response\n        name String\n        returns name\n      specification T\n        when Routed\n";
        var snapshot = new McpSnapshot([Document("model.play", source)]);
        snapshot.Compilation.Success.ShouldBeTrue();
        foreach (var (address, kind) in new[] { ("M.F.S.Routed", "Command"), ("M.F.S.T", "Specification"), ("M.F.S", "Slice") })
        {
            var readiness = Details(snapshot, address, kind, "summary").GetProperty("executionReadiness").GetString();
            readiness.ShouldNotContain("event sources, streams and routes (#302)");
            readiness.ShouldEqual("Not admitted by any supported executable model (ESM) version yet (PLAY0268): operations and systems (#301); use Authoring validation.");
            readiness.ShouldNotContain("(#300/#303)");
        }
        Details(snapshot, "M.F.S.Operation", "Command", "summary").GetProperty("executionReadiness").GetString().ShouldContain("operations and systems (#301)");
        Details(snapshot, "M.F.S.Response", "Command", "response").GetProperty("executionReadiness").ValueKind.ShouldEqual(JsonValueKind.Null);
        Details(snapshot, "M.F.S.Response", "Command", "response").GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_order_unadmitted_features_independently_of_declaration_order(bool reverse)
    {
        var commands = new[]
        {
            "      command Routed\n        stream Account.Onboarding\n        handler\n          file Routed.cs\n",
            "      command Response\n        name String\n        returns name\n",
            "      command Operation\n        produces Send\n"
        };
        var source = "numbers exact\nsystem Mailer\n" + Sources + "module M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n" + string.Concat(reverse ? commands.Reverse() : commands);
        var snapshot = new McpSnapshot([Document("model.play", source)]);
        snapshot.Compilation.Success.ShouldBeTrue();
        const string expected = "Not admitted by any supported executable model (ESM) version yet (PLAY0268): exact numbers (#285), operations and systems (#301), command handlers; use Authoring validation.";
        snapshot.Index.Readiness.ModelExecutionReadiness.ShouldEqual(expected);
        Details(snapshot, "M.F.S", "Slice", "summary").GetProperty("executionReadiness").GetString().ShouldEqual(expected);
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
        Readiness(Details(snapshot, "M.F.S.C", "Command", "summary"));
        Readiness(Details(snapshot, "M.F.S.T", "Specification", "summary"));
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
        Readiness(Details(snapshot, "M.F.B.T", "Specification", "summary"), "command handlers");
        Readiness(Details(snapshot, "M.F.A", "Slice", "commands").GetProperty("items")[0], "command handlers");
    }

    [Fact]
    void should_list_operations_once_without_listing_admitted_streams_or_responses()
    {
        const string source = "system Mailer\n" + Sources + "module M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n      command Routed\n        stream Account.Onboarding\n        produces Send\n      specification T\n        when Routed\n";
        var snapshot = new McpSnapshot([Document("model.play", source)]);
        snapshot.Compilation.Success.ShouldBeTrue();
        foreach (var (address, kind) in new[] { ("M.F.S.Routed", "Command"), ("M.F.S.T", "Specification"), ("M.F.S", "Slice") })
        {
            var details = Details(snapshot, address, kind, "summary");
            details.GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
            details.GetProperty("executionReadiness").GetString().ShouldEqual("Not admitted by any supported executable model (ESM) version yet (PLAY0268): operations and systems (#301); use Authoring validation.");
        }
    }

    [Theory]
    [InlineData("", "        handler\n          file C.cs\n", "command handlers")]
    [InlineData("numbers exact\n", "", "exact numbers (#285)")]
    void should_not_claim_response_commands_with_handlers_or_exact_mode_are_admitted(string prefix, string handler, string feature)
    {
        var snapshot = new McpSnapshot([Document("model.play", prefix + "module M\n  feature F\n    slice StateChange S\n      command C\n        name String\n        returns name\n" + handler + "      specification T\n        when C\n          name = \"value\"\n        then returns \"value\"\n")]);
        snapshot.Compilation.Success.ShouldBeTrue();
        foreach (var (address, kind, view) in new[] { ("M.F.S.C", "Command", "response"), ("M.F.S.T", "Specification", "summary"), ("M.F.S", "Slice", "summary") })
        {
            var details = Details(snapshot, address, kind, view);
            details.GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
            details.GetProperty("executionReadiness").GetString().ShouldContain(feature);
            details.GetProperty("executionReadiness").GetString().ShouldNotContain("(#300/#303)");
        }
    }

    [Theory]
    [InlineData("not empty")]
    [InlineData("rule Check\n      file Check.cs")]
    [InlineData("```csharp\n    return true;\n    ```")]
    void should_disclose_generated_concepts_with_validation_rules_as_unadmitted(string rule)
    {
        var snapshot = new McpSnapshot([Document("model.play", "concept Id : Uuid\n  validate\n    " + rule + "\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        returns id\n      specification T\n        when C\n          for \"11111111-1111-1111-1111-111111111111\"\n        then returns \"11111111-1111-1111-1111-111111111111\"\n")]);
        snapshot.Compilation.Success.ShouldBeTrue();
        foreach (var (address, kind) in new[] { ("M.F.S.C", "Command"), ("M.F.S.T", "Specification"), ("M.F.S", "Slice") })
        {
            var details = Details(snapshot, address, kind, "summary");
            details.GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
            details.GetProperty("executionReadiness").GetString().ShouldContain("generated properties on concepts with validation rules");
        }
    }

    [Fact]
    void should_keep_property_path_routes_unadmitted()
    {
        const string source = "type Period\n  value String\neventsource Account\n  stream Ledger\n    streamId String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        period Period\n        stream Account.Ledger\n          streamId = period.value\n";
        var snapshot = new McpSnapshot([Document("model.play", source)]);
        snapshot.Compilation.Success.ShouldBeTrue();
        Readiness(Details(snapshot, "M.F.S.C", "Command", "summary"), "property-path stream id mappings (#407)");
        Details(snapshot, "M.F.S.C", "Command", "summary").GetProperty("executionReadiness").GetString().ShouldContain("PLAY0268");
        Readiness(Details(snapshot, "Account", "EventSource", "summary"));
    }

    static void Readiness(JsonElement details, string? feature = null)
    {
        details.GetProperty("syntaxOnly").GetBoolean().ShouldEqual(feature is not null);
        if (feature is null) details.GetProperty("executionReadiness").ValueKind.ShouldEqual(JsonValueKind.Null);
        else details.GetProperty("executionReadiness").GetString().ShouldContain(feature);
    }

    static JsonElement Details(McpSnapshot snapshot, string address, string kind, string view) => Json(McpDeclarationDetails.Read(snapshot, Json(new { address, kind, view }))).GetProperty("details");

    static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value, McpJson.Options);

    static WorkspaceDocument Document(string path, string source) => WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source));
}
