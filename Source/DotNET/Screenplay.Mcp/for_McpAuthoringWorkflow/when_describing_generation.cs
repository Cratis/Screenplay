// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_describing_generation : given.an_authoring_connection
{
    [Theory]
    [InlineData("uuid", 122)]
    [InlineData("uuidv7", 74)]
    [InlineData("ulid", 80)]
    [InlineData("typeid \"user\"", 74)]
    [InlineData("nanoid", 126)]
    [InlineData("nanoid 32 alphabet \"ab\" prefix \"ref_\"", 32)]
    [InlineData("bytes 8 hex case upper", 64)]
    void should_report_exact_entropy_and_authored_modifiers(string strategy, double bits)
    {
        Start($"concept Code : String\n  generate {strategy}\n");
        var details = Generation();
        details.GetProperty("entropyBits").GetDouble().ShouldEqual(bits);
        details.GetProperty("valuesAtOnePercentCollision").GetDouble().ShouldEqual(GenerationEntropy.ValuesAtOnePercentCollision(bits));
        details.GetProperty("executable").GetProperty("status").GetString().ShouldEqual("admitted");
        details.GetProperty("executable").GetProperty("diagnosticCodes").GetArrayLength().ShouldEqual(0);
        if (strategy.Contains("prefix", StringComparison.Ordinal)) details.GetProperty("modifiers").GetProperty("prefix").GetString().ShouldEqual("ref_");
        if (strategy.StartsWith("typeid", StringComparison.Ordinal)) details.GetProperty("modifiers").GetProperty("prefix").GetString().ShouldEqual("user");
        if (strategy.Contains("upper", StringComparison.Ordinal)) details.GetProperty("modifiers").GetProperty("case").GetString().ShouldEqual("upper");
        if (strategy == "nanoid") details.GetProperty("modifiers").TryGetProperty("size", out _).ShouldBeFalse();
        details.TryGetProperty("derivedRegex", out _).ShouldBeFalse();
        details.TryGetProperty("ruleProofStatus", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("pattern \"ORD-#{7}\"", "PLAY0268", "pattern", "ORD-#{7}")]
    [InlineData("random 1..9", "PLAY0268", "range", "1..9")]
    [InlineData("rule Make", "PLAY0268", "rule", "Make")]
    [InlineData("sequence", "PLAY0682", null, null)]
    void should_report_unknown_entropy_and_compiler_refusals(string strategy, string code, string? argument, string? value)
    {
        Start($"concept Code : {(strategy.StartsWith("random", StringComparison.Ordinal) ? "Int" : "String")}\n  generate {strategy}\n");
        var details = Generation();
        details.GetProperty("entropyBits").ValueKind.ShouldEqual(JsonValueKind.Null);
        details.GetProperty("valuesAtOnePercentCollision").ValueKind.ShouldEqual(JsonValueKind.Null);
        details.GetProperty("entropyUnknownReason").GetString().ShouldContain("unknown");
        details.GetProperty("executable").GetProperty("status").GetString().ShouldEqual("refused");
        details.GetProperty("executable").GetProperty("diagnosticCodes").EnumerateArray().Select(item => item.GetString()).ShouldContain(code);
        if (argument is not null)
        {
            details.GetProperty("previewArguments").GetProperty(argument).GetProperty("value").GetString().ShouldEqual(value);
            details.GetProperty("previewArguments").GetProperty(argument).GetProperty("executable").GetBoolean().ShouldBeFalse();
        }
    }

    [Fact]
    void should_not_report_implicit_generation_as_authored()
    {
        Start("concept Code : Uuid\n");
        Result("declaration-details", new { address = "Code", kind = "Concept" }).GetProperty("details").TryGetProperty("generation", out _).ShouldBeFalse();
    }

    [Fact]
    void should_report_rule_refusal_from_the_compiler()
    {
        Start("concept Code : String\n  generate nanoid\n  validate\n    min 1\n");
        Generation().GetProperty("executable").GetProperty("diagnosticCodes").EnumerateArray().Select(item => item.GetString()).ShouldContain("PLAY0268");
    }

    [Fact]
    void should_transport_the_largest_collision_count_as_a_json_number()
    {
        Start("concept Code : String\n  generate bytes 512 hex\n");
        var count = Generation().GetProperty("valuesAtOnePercentCollision");
        count.ValueKind.ShouldEqual(JsonValueKind.Number);
        count.GetRawText().ShouldContain("e615");
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("aa")]
    void should_describe_invalid_alphabets_without_losing_parser_diagnostics(string alphabet)
    {
        var opened = Start($"concept Code : String\n  generate nanoid 1 alphabet \"{alphabet}\"\n");
        var details = Generation();
        details.GetProperty("entropyBits").ValueKind.ShouldEqual(JsonValueKind.Null);
        details.GetProperty("valuesAtOnePercentCollision").ValueKind.ShouldEqual(JsonValueKind.Null);
        details.GetProperty("entropyUnknownReason").GetString().ShouldContain("authored inputs");
        details.GetProperty("executable").GetProperty("status").GetString().ShouldEqual("refused");
        details.GetProperty("executable").GetProperty("diagnosticCodes").EnumerateArray().Select(item => item.GetString()).ShouldContainOnly("PLAY0679");
        Page("executable-diagnostics", opened.GetProperty("revision").GetString()!).EnumerateArray().Select(item => item.GetProperty("code").GetString()).ShouldContainOnly("PLAY0679");
    }

    [Theory]
    [InlineData("numbers exact\n", "", "PLAY0268")]
    [InlineData("", "concept Other : String\n  generate nanoid 1 alphabet \"\"\n", "PLAY0679")]
    void should_not_claim_admission_after_an_unrelated_model_failure(string prefix, string otherSource, string code)
    {
        if (otherSource.Length > 0) File.WriteAllText(Path.Combine(RootPath, "other.play"), otherSource);
        var opened = Start($"{prefix}concept Code : String\n  generate uuid\n");
        var details = Generation();
        details.GetProperty("entropyBits").GetDouble().ShouldEqual(122);
        details.GetProperty("executable").GetProperty("status").GetString().ShouldEqual("notAssessed");
        details.GetProperty("executable").GetProperty("diagnosticCodes").GetArrayLength().ShouldEqual(0);
        details.GetProperty("executable").GetProperty("reason").GetString().ShouldContain("whole model");
        Page("executable-diagnostics", opened.GetProperty("revision").GetString()!).EnumerateArray().Select(item => item.GetProperty("code").GetString()).ShouldContain(code);
    }

    JsonElement Start(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();

        return Open();
    }

    JsonElement Generation() => Result("declaration-details", new { address = "Code", kind = "Concept", view = "summary" }).GetProperty("details").GetProperty("generation");
}
