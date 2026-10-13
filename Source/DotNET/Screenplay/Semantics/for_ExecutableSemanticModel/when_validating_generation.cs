// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_generation : Specification
{
    [Theory]
    [InlineData("nanoid", "size")]
    [InlineData("bytes", "count")]
    [InlineData("typeid", "prefix")]
    void should_refuse_missing_strategy_arguments(string kind, string missing)
    {
        var concept = Concept(new(kind));
        var error = Catch.Exception(() => Create(concept));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error!.Message.ShouldContain(missing);
    }

    [Theory]
    [InlineData("pattern")]
    [InlineData("random")]
    [InlineData("implementation")]
    [InlineData("sequence")]
    [InlineData("unknown")]
    void should_refuse_unadmitted_programmatic_strategies(string kind) => Catch.Exception(() => Create(Concept(new(kind)))).ShouldBeOfExactType<InvalidSemanticContract>();

    [Fact] void should_refuse_generation_below_v15() => Catch.Exception(() => Create(Concept(new("ulid")), SemanticVersion.V14)).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_v15_without_generation() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V15, SemanticVersion.V15, canonical_serialization_golden_vectors.CreateSemanticModelV14().Application)).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_rules_on_generated_concepts() => Catch.Exception(() => Create(Concept(new("ulid")) with { Validations = [new(default, SemanticValidationRuleKind.Minimum, SemanticValue.Number(1), null)] })).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_alphabet_on_uuid() => Catch.Exception(() => Create(Concept(new("uuid") { Alphabet = "ab" }))).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_case_on_typeid() => Catch.Exception(() => Create(Concept(new("typeid") { Prefix = "", Case = "lower" }))).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_case_on_base64url() => Catch.Exception(() => Create(Concept(new("bytes") { Count = 16, Encoding = "base64url", Case = "upper" }))).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_default_uuid_generation_member() => Catch.Exception(() => Create(Concept(new("uuid")) with { Primitive = SemanticPrimitiveType.Uuid })).ShouldBeOfExactType<InvalidSemanticContract>();

    [Fact]
    void should_reject_invalid_combinations_before_revision_verification_in_the_reader()
    {
        var model = Create(Concept(new("ulid")));
        var bytes = System.Text.Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(model));
        var malformed = System.Text.Encoding.UTF8.GetBytes(bytes.Replace("\"generation\":{\"kind\":\"ulid\"}", "\"generation\":{\"kind\":\"ulid\",\"alphabet\":\"ab\"}", StringComparison.Ordinal));
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(malformed));
        error!.Message.ShouldContain("modifiers");
    }

    static SemanticConcept Concept(SemanticGeneration generation) => new(SemanticId.Parse("sem1:" + new string('f', 64)), "GeneratedValue", SemanticPrimitiveType.Text, [], []) { Generation = generation };

    static ExecutableSemanticModel Create(SemanticConcept concept, SemanticVersion? version = null)
    {
        var semantic = version ?? SemanticVersion.V15;
        var application = canonical_serialization_golden_vectors.CreateSemanticModelV14().Application;
        return ExecutableSemanticModel.Create(new(semantic.Major, semantic.Minor), semantic, application with { Concepts = application.Concepts.Add(concept) });
    }
}
