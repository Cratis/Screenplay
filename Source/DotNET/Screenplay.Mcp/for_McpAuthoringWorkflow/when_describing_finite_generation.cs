// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_describing_finite_generation : given.an_authoring_connection
{
    [Theory]
    [InlineData("", null)]
    [InlineData(" checksum luhn", "luhn")]
    void should_count_distinct_pattern_values_without_counting_checksum_entropy(string suffix, string? checksum)
    {
        Start($"concept Code : String\n  generate pattern \"ORD-#{{7}}\"{suffix}\n");
        var details = Generation();
        details.GetProperty("arguments").GetProperty("pattern").GetString().ShouldEqual("ORD-#{7}");
        details.GetProperty("arguments").TryGetProperty("checksum", out var algorithm).ShouldEqual(checksum is not null);
        if (checksum is not null) algorithm.GetString().ShouldEqual(checksum);
        details.GetProperty("supportCount").ValueKind.ShouldEqual(JsonValueKind.Number);
        details.GetProperty("supportCount").GetRawText().ShouldEqual("10000000");
        AssertEntropy(details, new BigInteger(10_000_000));
        details.GetProperty("executable").GetProperty("status").GetString().ShouldEqual("admitted");
        details.TryGetProperty("entropyUnknownReason", out _).ShouldBeFalse();
    }

    [Fact]
    void should_use_the_minimum_support_over_the_utc_date_horizon()
    {
        Start("concept Code : String\n  generate pattern \"{MM}[AB]\"\n");
        var details = Generation();
        var count = GenerationEntropy.ExactSupportCount(new GenerationSyntax("pattern", default) { Pattern = "{MM}[AB]" }, DateTimeOffset.UtcNow.Year)!.Value;
        count.ShouldBeGreaterThan(BigInteger.Zero);
        details.GetProperty("supportCount").GetRawText().ShouldEqual(count.ToString(CultureInfo.InvariantCulture));
        AssertEntropy(details, count);
    }

    [Theory]
    [InlineData("1..9", 1L, 9L, "9")]
    [InlineData("-9223372036854775808..9223372036854775807", long.MinValue, long.MaxValue, "18446744073709551616")]
    void should_transport_exact_integer_ranges_and_support(string text, long minimum, long maximum, string support)
    {
        Start($"concept Code : Int\n  generate random {text}\n");
        var details = Generation();
        var range = details.GetProperty("arguments").GetProperty("range");
        range.GetProperty("text").GetString().ShouldEqual(text);
        range.GetProperty("minimum").GetInt64().ShouldEqual(minimum);
        range.GetProperty("maximum").GetInt64().ShouldEqual(maximum);
        details.GetProperty("supportCount").ValueKind.ShouldEqual(JsonValueKind.Number);
        details.GetProperty("supportCount").GetRawText().ShouldEqual(support);
        AssertEntropy(details, BigInteger.Parse(support, CultureInfo.InvariantCulture));
        details.GetProperty("executable").GetProperty("status").GetString().ShouldEqual("admitted");
    }

    [Theory]
    [InlineData("pattern \"*{64}*{64}*{64}*{63}\" checksum mod11", "PLAY0691")]
    [InlineData("pattern \"000000006-[AB]\" checksum mod11", "PLAY0692")]
    [InlineData("pattern \"[]\"", "PLAY0673")]
    void should_report_compiler_refusals_without_throwing_or_inventing_entropy(string strategy, string code)
    {
        var opened = Start($"concept Code : String\n  generate {strategy}\n");
        var details = Generation();
        details.GetProperty("entropyBits").ValueKind.ShouldEqual(JsonValueKind.Null);
        details.GetProperty("valuesAtOnePercentCollision").ValueKind.ShouldEqual(JsonValueKind.Null);
        details.TryGetProperty("supportCount", out _).ShouldBeFalse();
        details.GetProperty("executable").GetProperty("status").GetString().ShouldEqual("refused");
        details.GetProperty("executable").GetProperty("diagnosticCodes").EnumerateArray().Select(item => item.GetString()).ShouldContain(code);
        var diagnostic = Page("executable-diagnostics", opened.GetProperty("revision").GetString()!).EnumerateArray().First(item => item.GetProperty("code").GetString() == code);
        details.GetProperty("entropyUnknownReason").GetString().ShouldEqual(diagnostic.GetProperty("message").GetString());
    }

    [Theory]
    [InlineData("9..1")]
    [InlineData("0..9223372036854775808")]
    void should_preserve_unparseable_ranges_without_claiming_bounds(string text)
    {
        Start($"concept Code : Int\n  generate random {text}\n");
        var details = Generation();
        var range = details.GetProperty("arguments").GetProperty("range");
        range.GetProperty("text").GetString().ShouldEqual(text);
        range.TryGetProperty("minimum", out _).ShouldBeFalse();
        range.TryGetProperty("maximum", out _).ShouldBeFalse();
        details.GetProperty("entropyBits").ValueKind.ShouldEqual(JsonValueKind.Null);
        details.GetProperty("entropyUnknownReason").GetString().ShouldContain("inclusive, ordered Int64 range");
    }

    [Fact]
    void should_describe_an_opaque_rule()
    {
        Start("concept Code : String\n  generate rule Make\n    ```csharp\n    return value;\n    ```\n");
        var details = Generation();
        details.GetProperty("arguments").GetProperty("rule").GetString().ShouldEqual("Make");
        details.GetProperty("entropyBits").ValueKind.ShouldEqual(JsonValueKind.Null);
        details.GetProperty("valuesAtOnePercentCollision").ValueKind.ShouldEqual(JsonValueKind.Null);
        details.GetProperty("entropyUnknownReason").GetString().ShouldContain("implementation is opaque");
        details.GetProperty("executable").GetProperty("status").GetString().ShouldEqual("admitted");
    }

    static void AssertEntropy(JsonElement details, BigInteger count)
    {
        var bits = GenerationEntropy.SupportBits(count);
        details.GetProperty("entropyBits").GetDouble().ShouldEqual(bits);
        details.GetProperty("valuesAtOnePercentCollision").GetDouble().ShouldEqual(GenerationEntropy.ValuesAtOnePercentCollision(bits));
    }

    JsonElement Start(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();

        return Open();
    }

    JsonElement Generation() => Result("declaration-details", new { address = "Code", kind = "Concept", view = "summary" }).GetProperty("details").GetProperty("generation");
}
