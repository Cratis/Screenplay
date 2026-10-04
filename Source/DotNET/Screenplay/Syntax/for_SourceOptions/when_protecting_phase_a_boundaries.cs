// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Syntax.for_SourceOptions;

public class when_protecting_phase_a_boundaries
{
    readonly ScreenplayCompiler _compiler = new();

    [Theory]
    [InlineData("```embedded")]
    [InlineData("``` // not a closer")]
    [InlineData("````")]
    [InlineData("```custom")]
    void should_preserve_raw_legacy_fence_bodies(string embedded)
    {
        var body = $"var text = \"\"\"\n{embedded}\nnumbers exact\n\"\"\";";
        var result = _compiler.Parse(Model($"handler\n          ```csharp\n{body}\n          ```"));
        result.Success.ShouldBeTrue();
        Command(result.Value!).Handler!.Code!.Code.ShouldEqual(body);
        result.Value!.SourceOptions.ShouldEqual(SourceOptions.Legacy);
        SyntaxJson.Serialize(result.Value).GetRawText().ShouldNotContain("sourceOptions");
    }

    [Fact]
    void should_consume_complete_exact_condition_and_policy_operands()
    {
        using var vectors = JsonDocument.Parse(File.ReadAllText(Vectors()));
        foreach (var vector in vectors.RootElement.GetProperty("operands").EnumerateArray())
        {
            var text = vector.GetProperty("text").GetString()!;
            var expected = vector.GetProperty("success").GetBoolean();
            var condition = _compiler.Parse("numbers exact\n" + Model($"produces when amount == {text}\n          Added"));
            var policy = _compiler.Parse($"numbers exact\npolicy P\n  require claim \"limit\" matches {text}\n");
            condition.Success.ShouldEqual(expected);
            policy.Success.ShouldEqual(expected);
            if (!expected) continue;
            var numeric = vector.GetProperty("numeric").GetBoolean();
            (((ComparisonConditionSyntax)Command(condition.Value!).Produces.Single().When!).Right is LiteralExpressionSyntax { Value: ExactNumber }).ShouldEqual(numeric);
            (((ClaimConditionSyntax)policy.Value.Policies.Single().Condition!).Matches is LiteralExpressionSyntax { Value: ExactNumber }).ShouldEqual(numeric);
        }
    }

    [Theory]
    [InlineData("numbers legacy\nconcept B : Decimal\n")]
    [InlineData("numbers exact\nconcept B : Decimal\n")]
    [InlineData("numbers exact\nimport \"a.play\"\n")]
    void should_preserve_invalid_consensus_through_print_json_and_binding_in_both_orders(string other)
    {
        var first = _compiler.Parse("concept A : Decimal\n", "a.play");
        var second = _compiler.Parse(other, "b.play");
        foreach (var documents in new CompilationResult<ApplicationSyntax>[][] { [first, second], [second, first] })
        {
            var merged = PlayFolderMerge.Merge(documents);
            merged.Success.ShouldBeFalse();
            Catch.Exception(() => SyntaxJson.Serialize(merged.Value!)).ShouldBeOfExactType<InvalidSyntaxJson>();
            Catch.Exception(() => new ScreenplayPrinter().Print(merged.Value!)).ShouldBeOfExactType<InvalidSyntaxJson>();
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Numbers"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("input"), "input", "input.play", "concept A : Decimal\n");
            var binding = new SemanticModelBinder().Bind("Numbers", merged.Value!, SemanticDocumentSet.Create([document], catalog));
            binding.Success.ShouldBeFalse();
            binding.Value.ShouldBeNull();
            merged.Value!.Concepts.Single(concept => concept.Name == "A").Location.Path.ShouldEqual("a.play");
        }
    }

    [Fact]
    void should_preserve_legacy_structured_rhs_diagnostic_coordinates()
    {
        var parsed = _compiler.CompileProjection("projection P\n  from E\n    amount = literal {\"amount\":9007199254740993}\n");
        var diagnostic = parsed.Diagnostics.Single(value => value.Code == "PLAY0150");
        diagnostic.Location.Line.ShouldEqual(3);
        diagnostic.Location.Column.ShouldEqual(5);
    }

    [Theory]
    [InlineData("capture C\n  numbers exact\n", "PLAY0079")]
    [InlineData("capture C\n  nested item\n    numbers exact\n", "PLAY0091")]
    [InlineData("capture C\n  map\n    bad line\n", "PLAY0080")]
    [InlineData("capture C\n  append invalid\n", "PLAY0084")]
    void should_report_native_exact_capture_diagnostics(string text, string code)
    {
        var result = _compiler.CompileCapture("numbers exact\n" + text);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code);
    }

    [Fact]
    void should_combine_numeric_options_with_route_candidates_and_caller_registered_fences()
    {
        var compiler = new ScreenplayCompiler(new ScreenplayLanguageRegistry(["custom"]));
        var source = "numbers exact\npolicy P\n  ```custom\n```embedded\neventsource Foreign\n  stream Bad\nnumbers exact\n  ```\neventsource Orders\n  stream Changes\n" + Model("stream Orders.Changes\n        produces Added\n          amount = 9007199254740993");
        var result = compiler.Parse(source);
        result.Success.ShouldBeTrue();
        result.Value!.EventSources.Single().Name.ShouldEqual("Orders");
        Command(result.Value).Stream!.EventSource.ShouldEqual("Orders");
        ((ExactNumber)((LiteralExpressionSyntax)Command(result.Value).Produces.Single().Mappings.Single().Source).Value!).CanonicalText.ShouldEqual("9007199254740993");
        var files = new InMemoryPlayDocumentSource(new Dictionary<string, string>(StringComparer.Ordinal) { ["root.play"] = "import \"child.play\"\n", ["child.play"] = source });
        var (_, folder) = PlayApplicationAssembly.Compile(compiler, ["root.play"], files);
        folder.Success.ShouldBeTrue();
        folder.Value!.SourceOptions.ShouldEqual(SourceOptions.Exact);
        folder.Value.EventSources.Count().ShouldEqual(1);
    }

    [Fact]
    void should_match_native_int32_and_uint32_lexical_transport_rules()
    {
        using var vectors = JsonDocument.Parse(File.ReadAllText(Vectors()));
        foreach (var vector in vectors.RootElement.GetProperty("integers").EnumerateArray())
        {
            var text = vector.GetProperty("text").GetString();
            foreach (var unsigned in new[] { false, true })
            {
                var slice = unsigned
                    ? $"\"events\":[{{\"kind\":\"EventSyntax\",\"name\":\"Added\",\"generation\":{text}}}]"
                    : $"\"reactions\":[{{\"kind\":\"ReactionSyntax\",\"name\":\"R\",\"triggers\":[{{\"kind\":\"ReactionTriggerSyntax\",\"source\":{{\"kind\":\"IntervalTriggerSourceSyntax\",\"amount\":{text},\"unit\":\"Seconds\"}}}}]}}]";
                using var json = JsonDocument.Parse(App(slice));
                var error = Catch.Exception(() => SyntaxJson.Deserialize(json.RootElement));
                if (vector.GetProperty(unsigned ? "uint" : "int").GetBoolean()) error.ShouldBeNull();
                else error.ShouldBeOfExactType<InvalidSyntaxJson>();
            }
        }
    }

    [Fact]
    void should_normalize_only_nullable_native_collections()
    {
        using var nullable = JsonDocument.Parse("{\"kind\":\"ApplicationSyntax\",\"sourceOptions\":{\"numericMode\":\"exact\"},\"types\":null,\"personas\":null,\"seeds\":null}");
        var result = (ApplicationSyntax)SyntaxJson.Deserialize(nullable.RootElement);
        result.Types!.ShouldBeEmpty();
        result.Modules.ShouldBeEmpty();
        foreach (var member in new[] { "\"eventSources\":null", "\"eventSources\":[{\"kind\":\"EventSourceSyntax\",\"name\":\"Orders\",\"streams\":null}]" })
        {
            using var malformed = JsonDocument.Parse("{\"kind\":\"ApplicationSyntax\",\"sourceOptions\":{\"numericMode\":\"exact\"}," + member + "}");
            Catch.Exception(() => SyntaxJson.Deserialize(malformed.RootElement)).ShouldBeOfExactType<InvalidSyntaxJson>();
        }
    }

    [Fact]
    void should_enforce_native_production_and_wrapped_handler_invariants_on_read_and_write()
    {
        var at = SourceLocation.Start;
        var operation = new OperationSyntax("Send", "External", [], at);
        var valid = new ProducesSyntax("Send", null, [], at) { InlineOperation = operation };
        SyntaxJson.Serialize(valid);
        foreach (var invalid in new[]
        {
            valid with { When = new ComparisonConditionSyntax("amount", ComparisonOperator.Equal, new LiteralExpressionSyntax(true, at), at) },
            valid with { Event = "Other" },
            valid with { For = new LiteralExpressionSyntax("global", at) },
            valid with { Tags = [new(new LiteralExpressionSyntax("business", at), at)] },
            valid with { Mappings = [new("amount", new LiteralExpressionSyntax(true, at), at)] }
        })
        {
            Catch.Exception(() => SyntaxJson.Serialize(invalid)).ShouldBeOfExactType<InvalidSyntaxJson>();
        }

        foreach (var patch in new[]
        {
            "\"when\":{\"kind\":\"ComparisonConditionSyntax\",\"left\":\"amount\",\"operator\":\"Equal\",\"right\":{\"kind\":\"LiteralExpressionSyntax\",\"value\":true}}",
            "\"event\":\"Other\"",
            "\"for\":{\"kind\":\"LiteralExpressionSyntax\",\"value\":\"global\"}",
            "\"inlineEvent\":{\"kind\":\"EventSyntax\",\"name\":\"Send\"}"
        })
        {
            using var json = JsonDocument.Parse("{\"kind\":\"ProducesSyntax\",\"inlineOperation\":{\"kind\":\"OperationSyntax\",\"name\":\"Send\",\"uses\":\"External\"}," + (patch.StartsWith("\"event\"", StringComparison.Ordinal) ? patch : "\"event\":\"Send\"," + patch) + "}");
            Catch.Exception(() => SyntaxJson.Deserialize(json.RootElement)).ShouldBeOfExactType<InvalidSyntaxJson>();
        }

        var handler = new HandlerSyntax(new("send.cs", at), new("csharp", "Send();", at), at) { Implementation = new([], at) };
        Catch.Exception(() => SyntaxJson.Serialize(handler)).ShouldBeOfExactType<InvalidSyntaxJson>();
        using var conflicting = JsonDocument.Parse("{\"kind\":\"HandlerSyntax\",\"file\":{\"kind\":\"FileReferenceSyntax\",\"path\":\"send.cs\"},\"code\":{\"kind\":\"CodeBlockSyntax\",\"language\":\"csharp\",\"code\":\"Send();\"},\"implementation\":{\"kind\":\"ImplementationSyntax\"}}");
        Catch.Exception(() => SyntaxJson.Deserialize(conflicting.RootElement)).ShouldBeOfExactType<InvalidSyntaxJson>();
    }

    [Fact]
    void should_default_all_nested_source_roots_to_legacy_without_mode_inheritance()
    {
        foreach (var family in new[] { "projections", "captures", "specifications" })
        {
            var declaration = family switch
            {
                "projections" => "{\"kind\":\"ProjectionSyntax\",\"name\":\"P\",\"autoMap\":\"Inherit\"}",
                "captures" => "{\"kind\":\"CaptureSyntax\",\"name\":\"C\"}",
                _ => "{\"kind\":\"SpecificationSyntax\",\"name\":\"T\"}"
            };
            using var json = JsonDocument.Parse(App($"\"{family}\":[{declaration}]"));
            Catch.Exception(() => SyntaxJson.Deserialize(json.RootElement)).ShouldBeOfExactType<InvalidSyntaxJson>();
        }
    }

    [Theory]
    [InlineData("-0", 0d)]
    [InlineData("9007199254740993", 9007199254740992d)]
    [InlineData("100000000000000020", 100000000000000016d)]
    [InlineData("1.00000000000000000000000000001", 1d)]
    [InlineData("0.00000000000000000000000000001", 1e-29)]
    void should_preserve_source_backed_legacy_double_values_and_signed_zero(string token, double expected)
    {
        var parsed = _compiler.Parse(Model($"produces Added\n          amount = {token}"));
        parsed.Success.ShouldBeTrue();
        var literal = (LiteralExpressionSyntax)Command(parsed.Value!).Produces.Single().Mappings.Single().Source;
        literal.Value.ShouldBeOfExactType<double>();
        var value = (double)literal.Value!;
        value.ShouldEqual(expected);
        if (token == "-0") BitConverter.DoubleToInt64Bits(value).ShouldEqual(long.MinValue);
        SyntaxJson.Serialize(parsed.Value).GetRawText().ShouldNotContain("sourceOptions");
    }

    static string Model(string body) => $"module M\n  feature F\n    slice StateChange S\n      command C\n        {body}\n";
    static CommandSyntax Command(ApplicationSyntax app) => app.Modules.Single().Features.Single().Slices.Single().Commands.Single();
    static string App(string slice) => "{\"kind\":\"ApplicationSyntax\",\"sourceOptions\":{\"numericMode\":\"exact\"},\"modules\":[{\"kind\":\"ModuleSyntax\",\"name\":\"M\",\"isPlacement\":false,\"features\":[{\"kind\":\"FeatureSyntax\",\"name\":\"F\",\"isPlacement\":false,\"slices\":[{\"kind\":\"SliceSyntax\",\"name\":\"S\",\"type\":\"StateChange\"," + slice + "}]}]}]}";
    static string Vectors([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;
        return Path.Combine(directory!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "numeric-source-regressions.json");
    }
}
