// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_numeric_event_source_keys : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    [Theory]
    [InlineData("1", "1")]
    [InlineData("1.00", "1")]
    [InlineData("0.00", "0")]
    [InlineData("-0.00", "0")]
    [InlineData("-1.20", "-1.2")]
    void should_infer_a_text_source_before_history_and_action_projections(string key, string expected)
    {
        foreach (var step in new[] { "given", "when append" })
        {
            CompileSource($"{step} Arrived\n  id = {key}\n  other = {key}\nthen readmodel Seen\n  source = \"{expected}\"");
            Run("S").Passed.ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("1.0", "1")]
    [InlineData("1.00", "1")]
    [InlineData("1.20", "1.2")]
    [InlineData("0.00", "0")]
    [InlineData("-0.00", "0")]
    [InlineData("-1.20", "-1.2")]
    [InlineData("-0.1234567890123456789012345678", "-0.1234567890123456789012345678")]
    void should_align_the_inferred_destination_and_typed_context_without_losing_precision(string key, string expected)
    {
        CompileSource("given Arrived\n  id = 1\n  other = 1\nthen readmodel Seen\n  source = \"1\"");
        var inferred = SemanticCaptureEvaluation.TakeEventSourceKey(_plan, Fact(Number(key), Number(key), new SemanticNullValue()));
        inferred.Destination.ShouldEqual(new SemanticTextValue(expected));
        inferred.Context!.EventSource.Value.ShouldEqual(inferred.Destination);
        inferred.Context.EventSource.Type.ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
    }

    [Theory]
    [InlineData("1.00", "1")]
    [InlineData("-0.00", "0")]
    [InlineData("-1.20", "-1.2")]
    void should_accept_canonical_explicit_text_in_history_and_actions(string key, string source)
    {
        foreach (var step in new[] { "given", "when append" })
        {
            CompileSource($"{step} Arrived\n  for \"{source}\"\n  id = {key}\n  other = {key}\nthen readmodel Seen\n  source = \"{source}\"");
            Run("S").Passed.ShouldBeTrue();
            SemanticCaptureEvaluation.TakeEventSourceKey(_plan, Fact(Number(key), Number(key), new SemanticTextValue(source))).Destination.ShouldEqual(new SemanticTextValue(source));
        }
    }

    [Theory]
    [InlineData("01")]
    [InlineData("1.0")]
    [InlineData("1e0")]
    [InlineData(" 1 ")]
    [InlineData("other")]
    void should_not_normalize_opaque_explicit_text_sources(string source)
    {
        foreach (var step in new[] { "given", "when append" })
        {
            CompileSource($"{step} Arrived\n  for \"{source}\"\n  id = 1\n  other = 1\nthen readmodel Seen\n  source = \"{source}\"");
            Run("S").Execution.ShouldBeOfExactType<SemanticRejected>();
        }
    }

    [Theory]
    [InlineData("1.00", "1")]
    [InlineData("-0.00", "0")]
    [InlineData("-0.1234567890123456789012345678", "-0.1234567890123456789012345678")]
    void should_compare_numeric_sources_by_value_and_preserve_their_type(string key, string source)
    {
        CompileSource("given Arrived\n  id = 1\n  other = 1\nthen readmodel Seen\n  source = \"1\"");
        var fact = Fact(Number(key), Number(source), Number(source)) with
        {
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DecimalNumber), Number(source)))
        };
        SemanticCaptureEvaluation.TakeEventSourceKey(_plan, fact).ShouldEqual(fact);
        SemanticCaptureEvaluation.KeyDisagreesWithSource(fact, Number(key)).ShouldBeNull();
    }

    [Fact]
    void should_render_a_numeric_disagreement_canonically()
    {
        CompileSource("given Arrived\n  id = 1\n  other = 1\nthen readmodel Seen\n  source = \"1\"");
        var error = Catch.Exception(() => SemanticCaptureEvaluation.TakeEventSourceKey(_plan, Fact(Number("1.00"), Number("1.00"), new SemanticTextValue("other"))));
        error.Message.ShouldContain("source 'other' carries key '1'.");
    }

    SemanticFact Fact(SemanticValue key, SemanticValue other, SemanticValue destination)
    {
        var contract = _plan.Events.Values.Single(value => value.Name == "Arrived");
        return new(contract.Id, destination, [new(contract.Properties.Single(value => value.Name == "id").Id, key), new(contract.Properties.Single(value => value.Name == "other").Id, other)]);
    }

    static SemanticNumberValue Number(string value) => new(decimal.Parse(value, CultureInfo.InvariantCulture));

    void CompileSource(string steps)
    {
        Compile($$"""
            module Shipping
              feature Tracking
                slice Translate Receive
                  direction inbound
                  event Arrived from "carrier"
                    id Decimal
                    other Decimal
                  capture First
                    source events
                      from Arrived
                    key id is event source
                  capture Second
                    source events
                      from Arrived
                    key other is event source
                  readmodel Seen
                    source String
                  query SeenBySource => Seen?
                    by source String
                  projection SeenProjection => Seen
                    from Arrived
                      source = $eventSourceId
                  specification S
            {{string.Join('\n', steps.Split('\n').Select(line => "        " + line))}}
            """);
    }
}
