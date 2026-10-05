// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Syntax.for_ExactNumber;

public class when_reading_shared_boundary_vectors : Specification
{
    [Fact]
    void should_preserve_each_shared_token_and_refuse_each_unrepresentable_or_incomplete_token()
    {
        foreach (var vector in Vectors().GetProperty("cases").EnumerateArray())
        {
            var token = vector.GetProperty("token").GetString()!;
            var canonical = vector.GetProperty("canonical").GetString();
            ExactNumber.IsToken(token).ShouldEqual(vector.GetProperty("isToken").GetBoolean());
            ExactNumber.TryParse(token, out var number).ShouldEqual(canonical is not null);
            number?.CanonicalText.ShouldEqual(canonical);
            number?.ToString().ShouldEqual(canonical);
        }
    }

    [Fact]
    void should_normalize_hundreds_of_shared_signed_coefficient_scale_and_removable_zero_vectors()
    {
        var vectors = Vectors();
        var minimum = vectors.GetProperty("minimumScale").GetInt32();
        var maximum = vectors.GetProperty("maximumScale").GetInt32();
        var zeros = vectors.GetProperty("removableZeros").GetInt32();
        var count = 0;
        foreach (var coefficient in vectors.GetProperty("coefficients").EnumerateArray().Select(value => value.GetString()!))
        {
            for (var scale = minimum; scale <= maximum; scale++)
            {
                foreach (var sign in new[] { string.Empty, "-" })
                {
                    var canonical = sign + FixedPoint(coefficient, scale);
                    foreach (var token in new[] { canonical, $"{sign}{coefficient}e-{scale}", $"{sign}000{coefficient}{new string('0', zeros)}e-{scale + zeros}" })
                    {
                        ExactNumber.TryParse(token, out var number).ShouldBeTrue();
                        number!.CanonicalText.ShouldEqual(canonical);
                        ExactNumber.TryParse(canonical, out var reparsed).ShouldBeTrue();
                        number.ShouldEqual(reparsed);
                        number.GetHashCode().ShouldEqual(reparsed!.GetHashCode());
                        number.IsIntegral.ShouldEqual(scale == 0);
                        count++;
                    }
                }
            }
        }

        count.ShouldBeGreaterThan(690);
    }

    [Fact]
    void should_compare_exact_values_without_boxed_type_or_scale_identity()
    {
        foreach (var vector in Vectors().GetProperty("comparisons").EnumerateArray())
        {
            ExactNumber.TryParse(vector.GetProperty("left").GetString()!, out var left).ShouldBeTrue();
            ExactNumber.TryParse(vector.GetProperty("right").GetString()!, out var right).ShouldBeTrue();
            var order = vector.GetProperty("order").GetInt32();
            ExactMathFacts.Compare(left!, right!).ShouldEqual(order);
            ExactMathFacts.Compare(right!, left!).ShouldEqual(-order);
            ExactMathFacts.Equal(left, right).ShouldEqual(order == 0);
        }

        ExactMathFacts.Equal(0L, 0.000m).ShouldBeTrue();
        ExactMathFacts.Equal(1, 1.0m).ShouldBeTrue();
        ExactMathFacts.Equal(long.MaxValue, (decimal)long.MaxValue).ShouldBeTrue();
        ExactMathFacts.Equal(0d, 0m).ShouldBeFalse();
        ExactMathFacts.Equal(0f, 0m).ShouldBeFalse();
        ExactNumber.TryParse("9223372036854775808", out var integral).ShouldBeTrue();
        ExactMathFacts.IsIntegral(integral!).ShouldBeTrue();
        ExactNumber.TryParse("1.0000000000000000000000000001", out var fraction).ShouldBeTrue();
        ExactMathFacts.IsIntegral(fraction!).ShouldBeFalse();
    }

    [Fact]
    void should_bound_huge_exponents_and_normalize_long_removable_coefficients_without_rounding()
    {
        var exponent = new string('9', 10000);
        ExactNumber.TryParse("1e" + exponent, out _).ShouldBeFalse();
        ExactNumber.TryParse("1e-" + exponent, out _).ShouldBeFalse();
        ExactNumber.TryParse("-0e" + exponent, out var zero).ShouldBeTrue();
        zero!.CanonicalText.ShouldEqual("0");
        ExactNumber.IsToken("1e" + exponent + "foo").ShouldBeFalse();
        ExactNumber.TryParse("1" + new string('0', 10000) + "e-10000", out var one).ShouldBeTrue();
        one!.CanonicalText.ShouldEqual("1");
        ExactNumber.TryParse("0." + new string('0', 10000) + "1e10001", out var cancelled).ShouldBeTrue();
        cancelled!.CanonicalText.ShouldEqual("1");
        ExactNumber.TryParse("0." + new string('0', 10000) + "1", out _).ShouldBeFalse();
    }

    [Fact]
    void should_ignore_culture_for_every_shared_boundary()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "en-US", "nb-NO", "fr-FR", "ar-SA", "ja-JP" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                should_preserve_each_shared_token_and_refuse_each_unrepresentable_or_incomplete_token();
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    static JsonElement Vectors([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md")))
        {
            root = root.Parent;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "exact-number-codec.json")));
        return document.RootElement.Clone();
    }

    static string FixedPoint(string coefficient, int scale)
    {
        if (scale == 0)
        {
            return coefficient;
        }

        return coefficient.Length > scale ? coefficient.Insert(coefficient.Length - scale, ".")
            : "0." + new string('0', scale - coefficient.Length) + coefficient;
    }
}
