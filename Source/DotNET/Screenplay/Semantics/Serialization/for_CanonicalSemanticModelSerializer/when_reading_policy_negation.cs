// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_policy_negation : Specification
{
    const string Condition = "{\"kind\":\"not\",\"operand\":{\"kind\":\"authenticated\"}}";
    string _json;

    void Establish()
    {
        var v6 = canonical_serialization_golden_vectors.CreateSemanticModelV6();
        var application = v6.Application with { Policies = v6.Application.Policies.Add(new("PersonOnly", new SemanticNotPolicyCondition(new SemanticAuthenticatedCondition()))) };
        _json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(ExecutableSemanticModel.Create(LanguageVersion.V7, SemanticVersion.V7, application)));
    }

    [Fact] void should_round_trip_the_unary_variant() => Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(_json)))).ShouldEqual(_json);
    [Fact] void should_refuse_a_missing_operand() => Refuse("{\"kind\":\"not\"}");
    [Fact] void should_refuse_a_null_operand() => Refuse("{\"kind\":\"not\",\"operand\":null}");
    [Fact] void should_refuse_a_duplicate_operand() => Refuse("{\"kind\":\"not\",\"operand\":{\"kind\":\"authenticated\"},\"operand\":{\"kind\":\"authenticated\"}}");
    [Fact] void should_refuse_an_unknown_member() => Refuse("{\"kind\":\"not\",\"operand\":{\"kind\":\"authenticated\"},\"extra\":true}");
    [Fact] void should_refuse_an_extraneous_role() => Refuse("{\"kind\":\"not\",\"operand\":{\"kind\":\"authenticated\"},\"role\":\"Admin\"}");
    [Fact] void should_refuse_an_opaque_operand() => Refuse("{\"kind\":\"not\",\"operand\":{\"kind\":\"opaque\"}}");
    [Fact] void should_refuse_an_operand_on_another_variant() => Refuse("{\"kind\":\"authenticated\",\"operand\":{\"kind\":\"authenticated\"}}");

    [Fact]
    void should_refuse_negation_in_a_pre_v7_contract()
    {
        var model = SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(_json));
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V6, SemanticVersion.V6, model.Application)).ShouldBeOfExactType<InvalidSemanticContract>();
        var preV7 = _json.Replace("\"schemaVersion\":7", "\"schemaVersion\":6", StringComparison.Ordinal)
            .Replace("\"languageVersion\":\"7.0\"", "\"languageVersion\":\"6.0\"", StringComparison.Ordinal)
            .Replace("\"semanticVersion\":\"7.0\"", "\"semanticVersion\":\"6.0\"", StringComparison.Ordinal);
        Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(preV7))).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_refuse_a_programmatic_opaque_operand()
    {
        var v6 = canonical_serialization_golden_vectors.CreateSemanticModelV6();
        var application = v6.Application with { Policies = v6.Application.Policies.Add(new("Unsafe", new SemanticNotPolicyCondition(new SemanticOpaquePolicyCondition(new string('a', 64))))) };
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V7, SemanticVersion.V7, application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    void Refuse(string condition) => Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(_json.Replace(Condition, condition, StringComparison.Ordinal)))).ShouldBeOfExactType<InvalidSemanticContract>();
}
