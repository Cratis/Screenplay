// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_enum_member_numbers : Specification
{
    static readonly SemanticConcept _enum = new(Id(2), "Status", SemanticPrimitiveType.Text, ["draft", "issued"], []) { Numbers = [-1, 4] };

    [Fact]
    void should_round_trip_numbers_as_the_last_concept_member()
    {
        var model = Create(_enum);
        var bytes = SemanticModelSerializer.Serialize(model);
        Encoding.UTF8.GetString(bytes).ShouldContain("\"values\":[\"draft\",\"issued\"],\"validations\":[],\"numbers\":[-1,4]");
        var read = SemanticModelSerializer.Deserialize(bytes);
        read.Application.Concepts.Single().Numbers!.Value.ShouldContainOnly(-1, 4);
        read.Revision.ShouldEqual(model.Revision);
    }

    [Fact] void should_accept_int32_boundaries() => Create(_enum with { Numbers = [int.MinValue, int.MaxValue] }).SemanticVersion.ShouldEqual(SemanticVersion.V12);
    [Fact] void should_refuse_programmatic_numbers_on_a_non_enum() => Refuse(_enum with { Values = [] });
    [Fact] void should_refuse_programmatic_numbers_on_a_non_text_concept() => Refuse(_enum with { Primitive = SemanticPrimitiveType.WholeNumber });
    [Fact] void should_refuse_programmatic_length_mismatch() => Refuse(_enum with { Numbers = [4] });
    [Fact] void should_refuse_programmatic_duplicates() => Refuse(_enum with { Numbers = [4, 4] });
    [Fact] void should_refuse_programmatic_positional_numbers() => Refuse(_enum with { Numbers = [0, 1] });
    [Fact] void should_refuse_programmatic_default_numbers() => Refuse(_enum with { Numbers = default(ImmutableArray<int>) });
    [Fact] void should_refuse_v12_without_numbered_members() => Refuse(_enum with { Numbers = null });

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    void should_refuse_numbers_in_every_older_contract(uint version)
    {
        Catch.Exception(() => ExecutableSemanticModel.Create(new(version, 0), new(version, 0), Application(_enum))).ShouldBeOfExactType<InvalidSemanticContract>();
        var json = Json().Replace("\"schemaVersion\":12", $"\"schemaVersion\":{version}", StringComparison.Ordinal)
            .Replace("\"12.0\"", $"\"{version}.0\"", StringComparison.Ordinal);
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldContain("numbers");
    }

    [Theory]
    [InlineData("[2147483648,4]")] [InlineData("[-2147483649,4]")]
    [InlineData("[1.5,4]")] [InlineData("[\"1\",4]")] [InlineData("[null,4]")]
    [InlineData("null")] [InlineData("[4]")] [InlineData("[4,4]")] [InlineData("[0,1]")] [InlineData("[]")]
    void should_refuse_invalid_json_numbers(string numbers)
    {
        var json = Json().Replace("\"numbers\":[-1,4]", $"\"numbers\":{numbers}", StringComparison.Ordinal);
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldNotContain("computed revision");
    }

    [Theory]
    [InlineData("\"values\":[\"draft\",\"issued\"]", "\"values\":[]")]
    [InlineData("\"primitive\":\"string\"", "\"primitive\":\"boolean\"")]
    void should_refuse_json_numbers_without_enum_values(string original, string replacement) =>
        Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(Json().Replace(original, replacement, StringComparison.Ordinal)))).ShouldBeOfExactType<InvalidSemanticContract>();

    static void Refuse(SemanticConcept concept) => Catch.Exception(() => Create(concept)).ShouldBeOfExactType<InvalidSemanticContract>();
    static string Json() => Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(Create(_enum)));
    static ExecutableSemanticModel Create(SemanticConcept concept) => ExecutableSemanticModel.Create(LanguageVersion.V12, SemanticVersion.V12, Application(concept));
    static SemanticApplication Application(SemanticConcept concept) => new(Id(1), "Enums", [concept], [], []);
    static SemanticId Id(int value) => SemanticId.Parse($"sem1:{value:x64}");
}
