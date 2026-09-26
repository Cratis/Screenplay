// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Xunit.Abstractions;

namespace Cratis.Screenplay.Parsing.for_ExpressionParser;

public partial class when_differentially_round_tripping_numeric_literals(ITestOutputHelper output)
{
    [Fact]
    public void should_preserve_every_value_and_every_faithful_main_representation()
    {
        var corpus = Corpus().Distinct().ToArray();
        Assert.True(corpus.Length >= 500, $"Only {corpus.Length} cases generated.");
        var mainAccepted = 0;
        var mainFaithful = 0;
        var newlyExact = 0;
        var mainFallback = 0;
        var nonFinite = 0;
        var faithfulPrintChanged = 0;
        var deviations = new List<string>();

        foreach (var source in corpus)
        {
            var parsed = ExpressionParser.ParseLiteral(source, SourceLocation.Start);
            Assert.NotNull(parsed);
            var value = parsed.Value!;
            var printed = ScreenplaySyntaxText.Expression(parsed);
            var reparsed = ExpressionParser.ParseLiteral(printed, SourceLocation.Start);
            Assert.NotNull(reparsed);
            Assert.Equal(value.GetType(), reparsed.Value!.GetType());
            Assert.Equal(value, reparsed.Value);

            if (value is double number && !double.IsFinite(number))
            {
                // The typed-JSON contract has never admitted non-finite JSON numbers.
                nonFinite++;
            }
            else
            {
                var typed = SyntaxJson.Serialize(parsed);
                var restored = (LiteralExpressionSyntax)SyntaxJson.Deserialize(typed);
                Assert.Equal(value.GetType(), restored.Value!.GetType());
                Assert.Equal(value, restored.Value);
                var serializedAgain = SyntaxJson.Serialize(restored);
                var readAgain = (LiteralExpressionSyntax)SyntaxJson.Deserialize(serializedAgain);
                Assert.Equal(value.GetType(), readAgain.Value!.GetType());
                Assert.Equal(value, readAgain.Value);

                var ordinary = JsonSerializer.Deserialize<JsonElement>($"{{\"kind\":\"LiteralExpressionSyntax\",\"value\":{source}}}");
                var fromJson = (LiteralExpressionSyntax)SyntaxJson.Deserialize(ordinary);
                Assert.Equal(value.GetType(), fromJson.Value!.GetType());
                Assert.Equal(value, fromJson.Value);
            }

            // Main's ExpressionParser.NumberRegex + double.Parse; exponent-only forms were not
            // source numeric literals on main and are still covered by the branch round trips above.
            if (!MainNumber().IsMatch(source))
            {
                continue;
            }

            var previous = double.Parse(source, CultureInfo.InvariantCulture);
            mainAccepted++;
            if (double.IsFinite(previous) && Faithful(source, previous))
            {
                mainFaithful++;
                Assert.IsType<double>(value);
                Assert.Equal(previous, value);
                var mainPrinted = MainPrint(previous);
                if (mainPrinted == printed || FaithfulPrinted(mainPrinted, previous))
                {
                    Assert.Equal(mainPrinted, printed);
                }
                else
                {
                    faithfulPrintChanged++;
                    if (faithfulPrintChanged <= 3)
                    {
                        deviations.Add($"{source}: faithful main Double prints {mainPrinted}, but this spelling is not its exact binary value; branch prints {printed} to preserve type+value on reparse");
                    }
                }

                // Chronicle origin/main ProjectionDefinitionSyntaxVisitor.FormatLiteralForStorage:
                // double -> number.ToString(InvariantCulture), otherwise Convert.ToString(InvariantCulture).
                Assert.Equal(previous.ToString(CultureInfo.InvariantCulture), ChronicleStorage(value));
            }
            else if (value is double)
            {
                mainFallback++;
            }
            else
            {
                newlyExact++;
                if (deviations.Count < 12)
                {
                    deviations.Add($"{source}: main {previous:R}, branch {value} ({value.GetType().Name}); exact authored value replaces rounding");
                }
            }
        }

        output.WriteLine($"corpus={corpus.Length}; mainAccepted={mainAccepted}; mainFaithfulValueAndStorageUnchanged={mainFaithful}; faithfulPrintChanged={faithfulPrintChanged}; newlyExact={newlyExact}; doubleFallback={mainFallback}; nonFiniteJsonExcluded={nonFinite}");
        foreach (var deviation in deviations)
        {
            output.WriteLine(deviation);
        }
    }

    static IEnumerable<string> Corpus()
    {
        var boundaries = new[]
        {
            0L, 1L, 10L, 9007199254740992L, 144115188075855872L,
            1152921504606846976L, long.MaxValue, long.MinValue
        };
        foreach (var boundary in boundaries)
        {
            var center = new BigInteger(boundary);
            for (var delta = -12; delta <= 12; delta++)
            {
                var number = (center + delta).ToString(CultureInfo.InvariantCulture);
                yield return number;
                yield return number + ".0";
                yield return number + ".0000000000000000000000000000001";
            }
        }

        foreach (var fraction in new[] { "0.1", "0.2", "0.3", "0.5", "0.00005", "0.123456789012345678901234567890", "1.00000000000000000000000000001", "10.50" })
        {
            yield return fraction;
            yield return "-" + fraction;
            yield return fraction + "00";
        }

        for (var exponent = 5; exponent <= 29; exponent++)
        {
            yield return "1e-" + exponent;
            yield return "-1E-" + exponent;
            yield return "0." + new string('0', exponent - 1) + "1";
            yield return "-0." + new string('0', exponent - 1) + "1";
        }

        foreach (var text in new[] { "-0", "-0.0", "0.000", "1e17", "1E+20", "1e30", "1e308", "1e400", "-1e400", "1" + new string('0', 309), "-1" + new string('0', 309), "9007199254740993." + new string('0', 130) })
        {
            yield return text;
        }
    }

    [GeneratedRegex(@"^-?\d+(\.\d+)?$", RegexOptions.None, 1000)]
    private static partial Regex MainNumber();

    static bool FaithfulPrinted(string printed, double number) => MainNumber().IsMatch(printed) && Faithful(printed, number);

    static string MainPrint(double number) =>
        number == Math.Floor(number) && number >= long.MinValue && number < 9223372036854775808d
            ? ((long)number).ToString(CultureInfo.InvariantCulture)
            : number.ToString(CultureInfo.InvariantCulture);

    static string ChronicleStorage(object? value) => value is double number
        ? number.ToString(CultureInfo.InvariantCulture)
        : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    static bool Faithful(string text, double number)
    {
        var parts = text.Split('.');
        var digits = string.Concat(parts);
        var authored = BigInteger.Parse(digits, CultureInfo.InvariantCulture);
        var decimalScale = parts.Length == 2 ? parts[1].Length : 0;
        var bits = BitConverter.DoubleToUInt64Bits(number);
        var exponentBits = (int)((bits >> 52) & 0x7ff);
        var significand = new BigInteger(bits & 0x000f_ffff_ffff_ffffUL);
        var binaryExponent = exponentBits == 0 ? -1074 : exponentBits - 1075;
        if (exponentBits != 0)
        {
            significand += BigInteger.One << 52;
        }

        if ((bits >> 63) != 0)
        {
            significand = -significand;
        }

        var decimalDenominator = BigInteger.Pow(10, decimalScale);
        return binaryExponent >= 0
            ? authored == (significand << binaryExponent) * decimalDenominator
            : authored * (BigInteger.One << -binaryExponent) == significand * decimalDenominator;
    }
}
