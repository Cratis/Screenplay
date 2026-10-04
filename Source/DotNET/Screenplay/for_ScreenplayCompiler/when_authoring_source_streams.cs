// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_authoring_source_streams : given.a_compiler
{
    const string Prefix = "concept AccountId : Uuid\nconcept Month : String\neventsource Account\n  identifier AccountId\n  stream Transactions\n    streamId Month\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id AccountId identifier\n        month Month\n";
    const string Route = "        stream Account.Transactions\n          streamId = month\n";

    [Theory]
    [InlineData("eventsource A\n  stream S\n  stream S", "PLAY0503")]
    [InlineData("eventsource A\neventsource A", "PLAY0503")]
    [InlineData("eventsource A\n  identifier String optional", "PLAY0503")]
    [InlineData("eventsource A\n  identifier String[]", "PLAY0503")]
    [InlineData("type Composite\n  value String\neventsource A\n  identifier Composite", "PLAY0503")]
    [InlineData("eventsource A\n  stream S\n    streamId Int", "PLAY0506")]
    [InlineData("eventsource A\n  stream S\n    streamId Decimal", "PLAY0506")]
    [InlineData("eventsource A\n  stream S\n    streamId Bool", "PLAY0506")]
    [InlineData("eventsource A\n  stream S\n    streamId DateTime", "PLAY0506")]
    [InlineData(Prefix + "        stream Account.Missing", "PLAY0504")]
    [InlineData(Prefix + "        stream Account.Transactions", "PLAY0504")]
    [InlineData(Prefix + "        stream Account.Transactions\n          streamId = missing", "PLAY0504")]
    [InlineData(Prefix + "        stream Account.Transactions\n          streamId = id", "PLAY0504")]
    [InlineData(Prefix + "        stream Account.Transactions\n          streamId = 42", "PLAY0504")]
    [InlineData(Prefix + Route + Route, "PLAY0504")]
    void should_report_known_invalid_declarations_or_routes(string source, string code) => _compiler.Compile(source).Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeTrue();

    [Theory]
    [InlineData("String")]
    [InlineData("Uuid")]
    [InlineData("Month")]
    [InlineData("Label")]
    [InlineData("Key")]
    void should_accept_portable_stream_id_types_without_executing_formatters(string type) => _compiler.Compile("concept Month : Int\nconcept Label : String\nconcept Key : Uuid\neventsource A\n  stream S\n    streamId " + type).Success.ShouldBeTrue();

    [Fact]
    void should_preserve_integer_concept_mapping_rules_without_admitting_bare_integer_declarations()
    {
        var source = Prefix.Replace("concept Month : String", "concept Month : Int", StringComparison.Ordinal);
        _compiler.Compile(source + Route).Success.ShouldBeTrue();
        _compiler.Compile(source + Route.Replace("streamId = month", "streamId = 42", StringComparison.Ordinal)).Success.ShouldBeTrue();
        _compiler.Compile(source.Replace("month Month", "month Int", StringComparison.Ordinal) + Route).Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0504").ShouldBeTrue();
    }

    [Theory]
    [InlineData("ß")]
    [InlineData("\u0301")]
    [InlineData("\u0661")]
    [InlineData("\u203F")]
    void should_keep_supported_unicode_identifier_continuations_and_exact_utf16_route_spans(string suffix)
    {
        var source = Prefix.Replace("Account", "Account" + suffix, StringComparison.Ordinal).Replace("Month", "Month" + suffix, StringComparison.Ordinal).Replace("Transactions", "Transactions" + suffix, StringComparison.Ordinal).Replace("month", "month" + suffix, StringComparison.Ordinal) +
            Route.Replace("Account", "Account" + suffix, StringComparison.Ordinal).Replace("Transactions", "Transactions" + suffix, StringComparison.Ordinal).Replace("month", "month" + suffix, StringComparison.Ordinal);
        var result = _compiler.Compile(source);
        result.Success.ShouldBeTrue();
        var route = Command(result.Value!).Stream!;
        route.ReferenceLength.ShouldEqual($"Account{suffix}.Transactions{suffix}".Length);
        route.StreamId!.SourceLength.ShouldEqual($"month{suffix}".Length);
    }

    [Fact]
    void should_leave_unknown_import_shapes_unresolved() => _compiler.Compile("import Contracts.Unknown\neventsource A\n  identifier Unknown\n  stream S\n    streamId Unknown").Diagnostics.ShouldBeEmpty();

    [Fact]
    void should_allow_handler_routes_without_produced_events()
    {
        _compiler.Compile(Prefix + Route + "        handler\n          file C.cs").Success.ShouldBeTrue();
        _compiler.Compile(Prefix + Route + "        handler\n          file C.cs\n        produces event Recorded").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.CommandWithProducesAndHandler).ShouldBeTrue();
    }

    [Fact]
    void should_not_capture_production_payload_keywords()
    {
        var result = _compiler.Compile(Prefix + "        produces event Recorded\n          stream String = month\n          streamId String = month\n          eventsource String = month\n          identifier String = month\n          from String = month");
        var command = Command(result.Value!);
        command.Stream.ShouldBeNull();
        string.Join(',', command.Produces.Single().Mappings.Select(mapping => mapping.Property)).ShouldEqual("stream,streamId,eventsource,identifier,from");
    }

    [Fact]
    void should_preserve_descriptions_pins_comments_crlf_and_exact_mapping_spans()
    {
        const string Source = "// root\r\neventsource Account // source\r\n  description \"Account\" // description\r\n  id \"OldAccount\" // pin\r\n  identifier AccountId // type\r\n  stream Transactions // stream\r\n    description\r\n      ```markdown\r\n      History\r\n      ```\r\n    id \"OldTransactions\"\r\n    streamId Month // key\r\nconcept AccountId : Uuid\r\nconcept Month : String\r\nmodule M\r\n  feature F\r\n    slice StateChange S\r\n      command C\r\n        month Month\r\n        stream Account.Transactions // route\r\n          streamId = month // mapping\r\n";
        var parsed = _compiler.Parse(Source, "input.play");
        parsed.Success.ShouldBeTrue();
        var route = Command(parsed.Value!).Stream!;
        route.ReferenceLocation.ShouldEqual(new SourceLocation(20, 16, "input.play"));
        route.ReferenceLength.ShouldEqual("Account.Transactions".Length);
        route.StreamId!.SourceLocation.ShouldEqual(new SourceLocation(21, 22, "input.play"));
        route.StreamId.SourceLength.ShouldEqual(5);
        var decoded = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed.Value!));
        SyntaxJson.StructurallyEqual(parsed.Value!, decoded).ShouldBeTrue();
        var printed = new ScreenplayPrinter().Print(parsed.Value!);
        SyntaxJson.StructurallyEqual(parsed.Value!, _compiler.Parse(printed).Value!).ShouldBeTrue();
        foreach (var comment in new[] { "// root", "// source", "// description", "// pin", "// type", "// stream", "// key", "// route", "// mapping" }) printed.ShouldContain(comment);
    }

    [Fact]
    void should_default_old_json_without_changing_positional_construction()
    {
        using var old = JsonDocument.Parse("{\"kind\":\"ApplicationSyntax\",\"imports\":[],\"concepts\":[],\"policies\":[],\"modules\":[]}");
        ((ApplicationSyntax)SyntaxJson.Deserialize(old.RootElement)).EventSources.ShouldBeEmpty();
        var command = new CommandSyntax("C", [], null, [], [], null, SourceLocation.Start);
        command.Stream.ShouldBeNull();
        using var oldCommand = JsonDocument.Parse("{\"kind\":\"CommandSyntax\",\"name\":\"C\",\"authorize\":null,\"handler\":null}");
        ((CommandSyntax)SyntaxJson.Deserialize(oldCommand.RootElement)).Stream.ShouldBeNull();
        ((CommandSyntax)SyntaxJson.Deserialize(oldCommand.RootElement)).StreamCandidates.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_classify_native_imports_and_placed_files_without_file_order_dependence(bool reverse)
    {
        var texts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["root.play"] = "import \"sources.play\"\nmodule M\n  feature F\n    import \"barrel.play\"",
            ["sources.play"] = "concept AccountId : Uuid\nconcept Month : String\neventsource Account\n  identifier AccountId\n  stream Transactions\n    streamId Month",
            ["barrel.play"] = "import \"command.play\"",
            ["command.play"] = "slice StateChange S\n  command C\n    month Month\n    stream Account.Transactions\n      streamId = month"
        };
        var roots = reverse ? texts.Keys.Reverse() : texts.Keys;
        var (_, result) = PlayApplicationAssembly.Compile(_compiler, roots, new InMemoryPlayDocumentSource(texts));
        result.Success.ShouldBeTrue();
        Command(result.Value!).Stream!.EventSource.ShouldEqual("Account");
        var duplicate = new Dictionary<string, string>(texts, StringComparer.Ordinal) { ["duplicate.play"] = "eventsource Account\n  stream Other" };
        var (_, conflicted) = PlayApplicationAssembly.Compile(_compiler, duplicate.Keys, new InMemoryPlayDocumentSource(duplicate));
        conflicted.Value!.EventSources.Count().ShouldEqual(2);
        new EventSourceCatalog(conflicted.Value).Resolve("Account", "Transactions").Kind.ShouldEqual(EventSourceResolutionKind.Ambiguous);
        conflicted.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0504").ShouldBeTrue();
    }

    [Fact]
    void should_exclude_sources_beneath_conflicting_barrels_from_authority()
    {
        var texts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["root.play"] = "module One\n  import \"barrel.play\"\nmodule Two\n  import \"barrel.play\"",
            ["barrel.play"] = "import \"sources.play\"",
            ["sources.play"] = "eventsource Account\n  stream Transactions"
        };
        var (_, result) = PlayApplicationAssembly.Compile(_compiler, ["root.play"], new InMemoryPlayDocumentSource(texts));
        result.Success.ShouldBeFalse();
        result.Value!.EventSources.ShouldBeEmpty();
        new EventSourceCatalog(result.Value).Resolve("Account", "Transactions").Kind.ShouldEqual(EventSourceResolutionKind.NotFound);
    }

    [Fact]
    void should_match_unicode_word_characters_in_source_and_stream_names()
    {
        var result = _compiler.Compile((Prefix + Route).Replace("Account", "Accountß", StringComparison.Ordinal).Replace("Transactions", "Transαctions", StringComparison.Ordinal));
        result.Success.ShouldBeTrue();
        Command(result.Value!).Stream!.EventSource.ShouldEqual("Accountß");
        Command(result.Value!).Stream!.Stream.ShouldEqual("Transαctions");
    }

    [Theory]
    [InlineData("optional")]
    [InlineData("[]")]
    void should_not_lose_shape_flags_from_a_composite_mapping_parent(string flag)
    {
        var type = flag == "[]" ? "Input[]" : "Input optional";
        var source = "type Input\n  month Month\nconcept Month : String\neventsource A\n  stream S\n    streamId Month\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        input " + type + "\n        stream A.S\n          streamId = input.month";
        _compiler.Compile(source).Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0504").ShouldBeTrue();
    }

    static CommandSyntax Command(ApplicationSyntax application) => application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
}
