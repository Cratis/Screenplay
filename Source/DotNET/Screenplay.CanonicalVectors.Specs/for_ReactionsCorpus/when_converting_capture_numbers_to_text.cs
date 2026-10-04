// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_converting_capture_numbers_to_text : Specification
{
    static readonly (decimal Value, string Text)[] Numbers =
    [
        (1.00m, "1"),
        (-12.5000m, "-12.5"),
        (new decimal(0, 0, 0, true, 28), "0"),
        (79228162514264337593543950335m, "79228162514264337593543950335"),
        (0.0000000000000000000000000001m, "0.0000000000000000000000000001")
    ];

    [Fact]
    void should_make_templates_independent_of_decimal_scale() => Check("value = `${amount}`", (_, text) => text);
    [Fact]
    void should_make_translations_independent_of_decimal_scale() => Check("value = amount translate\n  \"{text}\" => translated", (_, _) => "translated");
    [Fact]
    void should_make_splits_independent_of_decimal_scale() => Check("split amount by \",\"\n  value\n  unused", (_, text) => text);

    [Fact]
    void should_make_value_transitions_independent_of_decimal_scale()
    {
        foreach (var (value, text) in Numbers)
        {
            var model = given.v6_regression_models.Compile($$"""
                module Billing
                  feature Import
                    slice Translate Records
                      capture Records
                        key id
                        append Seen
                          when amount from "-1" to "{{text}}"
                      event Seen
                      specification Present
                        given capture Records
                          id = "root"
                          amount = -1
                        when capture Records
                          id = "root"
                          amount = 1
                        then Seen
                """);
            var scaled = given.v6_regression_models.ChangeSpecification(model, specification => specification with
            {
                GivenCaptures = [specification.GivenCaptures.Single() with { Record = Amount(specification.GivenCaptures.Single().Record, -1.00m) }],
                WhenCapture = specification.WhenCapture! with { Record = Amount(specification.WhenCapture.Record, value) }
            });
            foreach (var run in given.v6_regression_models.Runs(scaled)) run.Passed.ShouldBeTrue();
        }
    }

    static void Check(string map, Func<decimal, string, string> expected)
    {
        foreach (var (value, text) in Numbers)
        {
            var operation = map.Replace("{text}", text, StringComparison.Ordinal);
            var model = given.v6_regression_models.Compile($$"""
                module Billing
                  feature Import
                    slice Translate Records
                      capture Records
                        key id
                        map
                {{string.Join('\n', operation.Split('\n').Select(line => "          " + line))}}
                        append Seen
                          value = $.value
                      event Seen
                        value String
                      specification Present
                        when capture Records
                          id = "root"
                          amount = 1
                        then Seen
                          value = "{{expected(value, text)}}"
                """);
            var scaled = given.v6_regression_models.ChangeSpecification(model, specification => specification with
            {
                WhenCapture = specification.WhenCapture! with { Record = Amount(specification.WhenCapture.Record, value) }
            });
            foreach (var run in given.v6_regression_models.Runs(scaled))
            {
                run.Passed.ShouldBeTrue();
                ((SemanticAccepted)run.Execution).Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text(expected(value, text)));
            }
        }
    }

    static SemanticCaptureRecord Amount(SemanticCaptureRecord record, decimal value) => record with
    {
        Fields = [.. record.Fields.Select(field => field.Name == "amount" ? field with { Value = SemanticValue.Number(value) } : field)]
    };
}
