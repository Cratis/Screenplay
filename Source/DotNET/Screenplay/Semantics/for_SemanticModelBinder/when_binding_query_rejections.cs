// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_query_rejections : Specification
{
    SemanticCompilation _bound;

    void Because()
    {
        var result = query_validation_source.Compile();
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _bound = result.Value!;
    }

    [Fact] void should_select_v18() => _bound.Model.SemanticVersion.ShouldEqual(SemanticVersion.V18);
    [Fact] void should_retain_the_invalid_argument_as_a_shape_valid_fixture() => Scenario.ThenQueries.Single().Key.ShouldEqual(SemanticValue.Text("A"));
    [Fact] void should_lower_a_request_with_no_success_results() => Scenario.ThenQueries.Single().Results.ShouldBeEmpty();
    [Fact] void should_retain_the_error_message() => Scenario.ThenErrors.Single().Message.ShouldEqual("Invoice number is too short");

    [Fact]
    void should_bind_both_query_spellings_to_identical_bytes()
    {
        var source = query_validation_source.Source.Replace("when query GetInvoice\n          number = \"A\"", "then query GetInvoice\n          arguments\n            number = \"A\"", StringComparison.Ordinal);
        var result = query_validation_source.Compile(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        SemanticModelSerializer.Serialize(result.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(_bound.Model));
    }

    [Theory]
    [InlineData("then result\n          number = \"AB\"")]
    [InlineData("then no result")]
    [InlineData("then denied")]
    [InlineData("then error")]
    [InlineData("then query GetInvoice\n          arguments\n            number = \"B\"")]
    void should_refuse_other_outcomes_or_multiple_calls(string outcome) => query_validation_source.Compile(query_validation_source.Source + "\n        " + outcome).Success.ShouldBeFalse();

    [Fact]
    void should_keep_bare_actionless_error_invalid() => query_validation_source.Compile(query_validation_source.Source.Replace("        when query GetInvoice\n          number = \"A\"\n", string.Empty, StringComparison.Ordinal)).Success.ShouldBeFalse();

    [Fact]
    void should_not_promote_an_ordinary_validated_lookup() => query_validation_source.Compile(query_validation_source.Source.Replace("then error \"Invoice number is too short\"", "then no result", StringComparison.Ordinal)).Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);

    SemanticSpecification Scenario => _bound.Model.Application.Modules[0].Features[0].Slices[0].Specifications[0];
}
