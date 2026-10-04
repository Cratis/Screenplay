// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_validating_condition_value_variants : Specification
{
    const string Source = """
        trigger Begin
          amount Decimal
          enabled Bool
        module Billing
          feature Guards
            slice Automation Guards
              reaction BatchHandler
                where amount > 0
                when Begin
                  produces Accepted
                    for "root"
              event Accepted
              specification Guard
                when trigger Begin
                  amount = 1
                  enabled = true
                then Accepted
                  for "root"
        """;

    [Fact]
    void should_reject_a_number_disguised_as_text_before_create_or_write_can_produce_unreadable_canonical_bytes()
    {
        var model = given.v6_regression_models.Compile(Source);
        var guard = (SemanticComparison)Trigger(model).Where!;
        var condition = guard with { Right = new(default, new SemanticNumberValue(0) { Kind = SemanticValueKind.Text }) };
        ExecutableSemanticModel? created = null;
        var createError = Catch.Exception(() => created = WithGuard(model, condition));
        byte[]? bytes = null;
        var writeError = created is null ? null : Catch.Exception(() => bytes = SemanticModelSerializer.Serialize(created));
        var readError = bytes is null ? null : Catch.Exception(() => SemanticModelSerializer.Deserialize(bytes));
        Assert.True(createError is InvalidSemanticContract,
            $"Create: {createError?.Message ?? "accepted fresh revision"}; Write: {writeError?.Message ?? (bytes is null ? "not reached" : "accepted")}; Read: {readError?.Message ?? "not reached"}");
        Reject(model, condition, "malformed");
    }

    [Fact]
    void should_validate_every_scalar_and_recursive_variant_before_inferring_either_constant_operand_type()
    {
        var model = given.v6_regression_models.Compile(Source);
        var property = model.Application.Triggers.Single().Properties[0].Id;
        var malformed = new List<SemanticValue>();
        foreach (var scalar in new SemanticValue[] { SemanticValue.Null, SemanticValue.Text("x"), SemanticValue.Number(1), SemanticValue.Boolean(true) })
        {
            malformed.AddRange(Enum.GetValues<SemanticValueKind>().Append((SemanticValueKind)123).Where(kind => kind != scalar.Kind).Select(kind => scalar with { Kind = kind }));
        }

        malformed.AddRange([
            new SemanticTextValue(null!),
            new SemanticArrayValue(default),
            new SemanticCompositeValue(default),
            new unknown_value(SemanticValueKind.Boolean),
            new SemanticArrayValue([new SemanticCompositeValue([new(property, new SemanticNumberValue(1) { Kind = SemanticValueKind.Boolean })])]),
            new SemanticCompositeValue([new(property, new SemanticArrayValue([new SemanticBooleanValue(true) { Kind = SemanticValueKind.Number }]))])
        ]);
        foreach (var value in malformed)
        {
            foreach (var left in new[] { false, true })
            {
                var bad = new SemanticConditionOperand(default, value);
                var good = new SemanticConditionOperand(default, SemanticValue.Boolean(true));
                var comparison = new SemanticComparison(left ? bad : good, SemanticComparisonOperator.Equal, left ? good : bad);
                Reject(model, comparison, "malformed");
                foreach (var op in Enum.GetValues<SemanticLogicalOperator>())
                {
                    var valid = new SemanticComparison(good, SemanticComparisonOperator.Equal, good);
                    Reject(model, new SemanticLogicalCondition(left ? comparison : valid, op, left ? valid : comparison), "malformed");
                }
            }
        }
    }

    [Fact]
    void should_reject_the_full_v6_golden_batch_handler_guard_with_a_malformed_constant()
    {
        var model = SemanticModelSerializer.Deserialize(global::Cratis.Screenplay.CanonicalVectors.Specs.given.canonical_serialization_golden_bytes.SemanticModelV6);
        var found = false;
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Reactions = [.. slice.Reactions.Select(reaction => reaction.Name == "BatchHandler" ? reaction with
                        {
                            Triggers = [.. reaction.Triggers.Select(trigger =>
                            {
                                if (trigger.Where is not SemanticComparison comparison) return trigger;
                                found = true;
                                return trigger with { Where = comparison with { Right = new(default, new SemanticNumberValue(0) { Kind = SemanticValueKind.Text }) } };
                            })]
                        } : reaction)]
                    })]
                })]
            })]
        };
        found.ShouldBeTrue();
        Catch.Exception(() => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_reject_recomputed_revision_reader_mutations_for_scalar_and_nested_logical_constants()
    {
        var model = given.v6_regression_models.Compile(Source);
        foreach (var logical in new[] { false, true })
        {
            foreach (var op in Enum.GetValues<SemanticLogicalOperator>())
            {
                foreach (var left in new[] { false, true })
                {
                    var constant = new SemanticConditionOperand(default, SemanticValue.Boolean(true));
                    var comparison = new SemanticComparison(constant, SemanticComparisonOperator.Equal, constant);
                    var valid = WithGuard(model, logical ? new SemanticLogicalCondition(comparison, op, comparison) : comparison);
                    var json = JsonNode.Parse(SemanticModelSerializer.Serialize(valid))!.AsObject();
                    json.Remove("revision");
                    var guard = json["application"]!["modules"]![0]!["features"]![0]!["slices"]![0]!["reactions"]![0]!["triggers"]![0]!["where"]!;
                    if (logical) guard = guard[left ? "left" : "right"]!;
                    guard[left ? "left" : "right"]!["value"]!["kind"] = "number";
                    var content = json.ToJsonString();
                    var revision = Revision(Encoding.UTF8.GetBytes(content));
                    var bytes = Encoding.UTF8.GetBytes(content.Replace(",\"application\":", $",\"revision\":\"{revision}\",\"application\":", StringComparison.Ordinal));
                    var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(bytes));
                    error.ShouldBeOfExactType<InvalidSemanticContract>();
                    error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
                }
            }
        }
    }

    [Fact]
    void should_preserve_valid_constants_and_existing_null_collection_and_optional_operand_rules()
    {
        var model = given.v6_regression_models.Compile(Source);
        foreach (var value in new[] { SemanticValue.Text("x"), SemanticValue.Number(0), SemanticValue.Boolean(true) })
        {
            var operand = new SemanticConditionOperand(default, value);
            var comparison = new SemanticComparison(operand, SemanticComparisonOperator.Equal, operand);
            foreach (var run in given.v6_regression_models.Runs(WithGuard(model, comparison))) run.Passed.ShouldBeTrue();
        }

        foreach (var value in new SemanticValue[] { SemanticValue.Null, new SemanticArrayValue([]), new SemanticCompositeValue([]) })
        {
            var operand = new SemanticConditionOperand(default, value);
            Reject(model, new SemanticComparison(operand, SemanticComparisonOperator.Equal, operand), "constants");
        }

        // Optional occurrence properties remain incompatible with conditions, even with scalar actuals.
        var application = model.Application with
        {
            Triggers = [model.Application.Triggers.Single() with
        {
            Properties = [.. model.Application.Triggers.Single().Properties.Select(property => property with { Type = property.Type with { IsOptional = true } })]
        }]
        };
        Catch.Exception(() => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    static SemanticReactionTrigger Trigger(ExecutableSemanticModel model) => model.Application.Modules.Single().Features.Single().Slices.Single().Reactions.Single().Triggers.Single();

    static SemanticApplication Application(ExecutableSemanticModel model, SemanticCondition condition)
    {
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var reaction = slice.Reactions.Single();
        return model.Application with { Modules = [module with { Features = [feature with { Slices = [slice with { Reactions = [reaction with { Triggers = [Trigger(model) with { Where = condition }] }] }] }] }] };
    }

    static ExecutableSemanticModel WithGuard(ExecutableSemanticModel model, SemanticCondition condition) => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, Application(model, condition));

    static void Reject(ExecutableSemanticModel model, SemanticCondition condition, string message)
    {
        var application = Application(model, condition);
        var created = Catch.Exception(() => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application));
        var invalid = (ExecutableSemanticModel)Activator.CreateInstance(
            typeof(ExecutableSemanticModel),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [model.LanguageVersion, model.SemanticVersion, model.Revision, application],
            null)!;
        var written = Catch.Exception(() => SemanticModelSerializer.Serialize(invalid));
        created.ShouldBeOfExactType<InvalidSemanticContract>();
        created.Message.ShouldContain(message);
        written.ShouldBeOfExactType<InvalidSemanticContract>();
        written.Message.ShouldContain(message);
        written.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }

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
