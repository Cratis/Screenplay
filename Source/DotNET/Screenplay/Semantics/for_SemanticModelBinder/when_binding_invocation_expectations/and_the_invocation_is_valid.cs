// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_invocation_expectations;

public class and_the_invocation_is_valid : given.an_invoice_closing_source
{
    CompilationResult<SemanticCompilation> _result;
    SemanticSpecification _specification;
    byte[] _bytes;

    void Because()
    {
        _result = Bind(With(
            """
            then invokes CloseInvoice
              reason = "paid"
            """));
        _specification = _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Closing").Specifications.Single();
        _bytes = SemanticModelSerializer.Serialize(_result.Value!.Model);
    }

    [Fact] void should_bind() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(value => value.Message)));
    [Fact] void should_select_esm_v34() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V34);
    [Fact] void should_bind_one_invocation() => _specification.ThenInvocations.Length.ShouldEqual(1);
    [Fact] void should_bind_only_the_stated_value() => _specification.ThenInvocations[0].Values.Single().Value.ShouldEqual(SemanticValue.Text("paid"));
    [Fact] void should_write_the_invocations() => System.Text.Encoding.UTF8.GetString(_bytes).ShouldContain("\"thenInvocations\":[{\"command\":");
    [Fact] void should_round_trip() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);
}
