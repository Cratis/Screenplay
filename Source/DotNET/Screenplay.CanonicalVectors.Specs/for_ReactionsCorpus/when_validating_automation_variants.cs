// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_validating_automation_variants : Specification
{
    const string Source = """
        trigger Begin
          id String
        module Billing
          feature Records
            slice Translate Records
              command Echo
                id String identifier
                step Int
                produces Changed
                  for id
                  step = step
                  at = $context.occurred
              reaction React
                when Begin
                  id
                  produces Changed
                    for id
                    step = 1
                    at = $context.occurred
                  invokes Echo
                    id = id
                    step = 1
              capture Records
                key id
                append Changed
                  step = 1
                  at = $context.occurred
              event Changed
                step Int
                at DateTime
              specification Present
                given clock "2026-10-02T09:00:00Z"
                given capture Records
                  id = "root"
                when capture Records
                  id = "root"
                  payload = 1
                then Changed
                  for "root"
                  step = 1
                  at = "2026-10-02T09:00:00Z"
        """;

    [Fact]
    void should_reject_every_wrong_scalar_discriminator_at_every_capture_record_level_on_create_and_serialize()
    {
        var model = given.v6_regression_models.Compile(Source);
        foreach (var scalar in new SemanticValue[] { SemanticValue.Null, SemanticValue.Text("x"), SemanticValue.Number(1), SemanticValue.Boolean(true) })
        {
            foreach (var kind in Enum.GetValues<SemanticValueKind>().Append((SemanticValueKind)123).Where(kind => kind != scalar.Kind))
            {
                foreach (var level in new[] { 0, 1, 2 })
                {
                    foreach (var given in new[] { false, true })
                    {
                        var field = Nested(new("payload", SemanticCaptureFieldKind.Value) { Value = scalar with { Kind = kind } }, level);
                        Reject(model, slice => slice with
                        {
                            Specifications = [slice.Specifications.Single() with
                        {
                            GivenCaptures = given ? [Record(slice.Specifications.Single().GivenCaptures.Single(), field)] : slice.Specifications.Single().GivenCaptures,
                            WhenCapture = given ? slice.Specifications.Single().WhenCapture : Record(slice.Specifications.Single().WhenCapture!, field)
                        }]
                        });
                    }
                }
            }
        }
    }

    [Fact]
    void should_reject_a_numeric_key_disguised_as_text_before_hashing_or_serializing()
    {
        var model = given.v6_regression_models.Compile(Source);
        Reject(model, slice => slice with
        {
            Specifications = [slice.Specifications.Single() with
        {
            WhenCapture = slice.Specifications.Single().WhenCapture! with
            {
                Record = new([new("id", SemanticCaptureFieldKind.Value) { Value = new SemanticNumberValue(1m) { Kind = SemanticValueKind.Text } }])
            }
        }]
        });
    }

    [Fact]
    void should_reject_malformed_structured_and_unknown_value_variants_instead_of_flattening_them()
    {
        var model = given.v6_regression_models.Compile(Source);
        foreach (var value in new SemanticValue[]
        {
            new SemanticTextValue(null!),
            new SemanticArrayValue([new SemanticNumberValue(1) { Kind = SemanticValueKind.Text }]),
            new SemanticArrayValue(default),
            new SemanticArrayValue([]) { Kind = SemanticValueKind.Text },
            new SemanticCompositeValue([new(model.Application.Modules.Single().Features.Single().Slices.Single().Events.Single().Properties[0].Id, new SemanticNumberValue(1) { Kind = SemanticValueKind.Text })]),
            new SemanticCompositeValue(default),
            new SemanticCompositeValue([]) { Kind = SemanticValueKind.Array },
            new unknown_value(SemanticValueKind.Number)
        })
        {
            Reject(model, slice => slice with
            {
                Specifications = [slice.Specifications.Single() with
            {
                WhenCapture = Record(slice.Specifications.Single().WhenCapture!, new("payload", SemanticCaptureFieldKind.Value) { Value = value })
            }]
            });
        }
    }

    [Fact]
    void should_reject_wrong_expression_discriminators_in_reaction_destinations_mappings_invocations_and_capture_mappings()
    {
        var model = given.v6_regression_models.Compile(Source);
        foreach (var kind in Enum.GetValues<SemanticExpressionKind>().Append((SemanticExpressionKind)123))
        {
            var trigger = model.Application.Modules.Single().Features.Single().Slices.Single().Reactions.Single().Triggers.Single();
            foreach (var expression in new[] { trigger.Produces.Single().Destination!, SemanticExpression.FromValue(SemanticValue.Text("root")) })
            {
                if (expression.Kind == kind) continue;
                Reject(model, slice => slice with
                {
                    Reactions = [slice.Reactions.Single() with { Triggers = [trigger with
                {
                    Produces = [trigger.Produces.Single() with { Destination = expression with { Kind = kind } }]
                }] }]
                });
            }

            foreach (var index in new[] { 0, 1 })
            {
                var mapping = trigger.Produces.Single().Mappings[index];
                if (mapping.Source.Kind == kind) continue;
                Reject(model, slice => slice with
                {
                    Reactions = [slice.Reactions.Single() with { Triggers = [trigger with
                {
                    Produces = [trigger.Produces.Single() with { Mappings = [.. trigger.Produces.Single().Mappings.Select(value => value == mapping ? value with { Source = value.Source with { Kind = kind } } : value)] }]
                }] }]
                });
                var capture = model.Application.Modules.Single().Features.Single().Slices.Single().Captures.Single();
                var captureMapping = capture.Appends.Single().Mappings[index];
                Reject(model, slice => slice with
                {
                    Captures = [capture with { Appends = [capture.Appends.Single() with
                {
                    Mappings = [.. capture.Appends.Single().Mappings.Select(value => value == captureMapping ? value with { Value = value.Value! with { Kind = kind } } : value)]
                }] }]
                });
            }

            var invocation = trigger.Invokes.Single();
            foreach (var mapping in invocation.Mappings.Where(mapping => mapping.Source.Kind != kind))
            {
                Reject(model, slice => slice with
                {
                    Reactions = [slice.Reactions.Single() with { Triggers = [trigger with
                {
                    Invokes = [invocation with { Mappings = [.. invocation.Mappings.Select(value => value == mapping ? value with { Source = value.Source with { Kind = kind } } : value)] }]
                }] }]
                });
            }
        }
    }

    [Fact]
    void should_reject_malformed_canonical_values_with_a_revision_recomputed_over_the_mutated_content()
    {
        var model = given.v6_regression_models.Compile(Source);
        foreach (var kind in new[] { "text", "boolean", "null", "array", "object", "unknown" })
        {
            var json = JsonNode.Parse(SemanticModelSerializer.Serialize(model))!.AsObject();
            json.Remove("revision");
            var field = json["application"]!["modules"]![0]!["features"]![0]!["slices"]![0]!["specifications"]![0]!["whenCapture"]!["record"]!["fields"]![1]!;
            field["value"]!["kind"] = kind;
            var content = json.ToJsonString();
            var revision = Revision(Encoding.UTF8.GetBytes(content));
            var bytes = Encoding.UTF8.GetBytes(content.Replace(",\"application\":", $",\"revision\":\"{revision}\",\"application\":", StringComparison.Ordinal));
            var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(bytes));
            error.ShouldBeOfExactType<InvalidSemanticContract>();
            error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        }
    }

    [Fact]
    void should_keep_all_valid_scalar_and_record_shapes_round_trippable()
    {
        var model = given.v6_regression_models.Compile(Source);
        foreach (var value in new SemanticValue[] { SemanticValue.Null, SemanticValue.Text("x"), SemanticValue.Number(1.00m), SemanticValue.Boolean(true) })
        {
            foreach (var level in new[] { 0, 1, 2 })
            {
                var application = Change(model, slice => slice with
                {
                    Specifications = [slice.Specifications.Single() with
                {
                    WhenCapture = Record(slice.Specifications.Single().WhenCapture!, Nested(new("payload", SemanticCaptureFieldKind.Value) { Value = value }, level))
                }]
                });
                var valid = ExecutableSemanticModel.Create(LanguageVersion.V6, SemanticVersion.V6, application);
                foreach (var run in given.v6_regression_models.Runs(valid)) run.Passed.ShouldBeTrue();
            }
        }
    }

    static void Reject(ExecutableSemanticModel model, Func<SemanticSlice, SemanticSlice> change)
    {
        var application = Change(model, change);
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V6, SemanticVersion.V6, application)).ShouldBeOfExactType<InvalidSemanticContract>();

        // Bypass Create solely to verify the public serialization admission boundary, not to manufacture
        // a stale-hash failure. Validation must reject before the serializer compares the revision.
        var invalid = (ExecutableSemanticModel)Activator.CreateInstance(
            typeof(ExecutableSemanticModel),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [LanguageVersion.V6, SemanticVersion.V6, model.Revision, application],
            null)!;
        var error = Catch.Exception(() => SemanticModelSerializer.Serialize(invalid));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }

    static SemanticApplication Change(ExecutableSemanticModel model, Func<SemanticSlice, SemanticSlice> change) => model.Application with
    {
        Modules = [.. model.Application.Modules.Select(module => module with { Features = [.. module.Features.Select(feature => feature with { Slices = [.. feature.Slices.Select(change)] })] })]
    };

    static SemanticSpecificationCapture Record(SemanticSpecificationCapture capture, SemanticCaptureField field) => capture with
    {
        Record = new([capture.Record.Fields.Single(value => value.Name == "id"), field])
    };

    static SemanticCaptureField Nested(SemanticCaptureField field, int level) => level switch
    {
        1 => new("nested", SemanticCaptureFieldKind.Record) { Record = new([field]) },
        2 => new("children", SemanticCaptureFieldKind.Records) { Records = [new([field])] },
        _ => field
    };

    static SemanticRevision Revision(byte[] content)
    {
        var domain = Encoding.UTF8.GetBytes("Cratis.Screenplay.SemanticRevision.v1");
        var bytes = new byte[8 + domain.Length + content.Length];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)domain.Length);
        domain.CopyTo(bytes, 4);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4 + domain.Length), (uint)content.Length);
        content.CopyTo(bytes, 8 + domain.Length);
        return SemanticRevision.Parse("rev1:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
    }

    sealed record unknown_value(SemanticValueKind Kind) : SemanticValue(Kind);
}
