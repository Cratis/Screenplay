// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_invocation_expectations;

public class and_no_invocation_is_stated : given.an_invoice_closing_source
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(With("then no events"));

    [Fact] void should_bind() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(value => value.Message)));
    [Fact] void should_keep_an_earlier_version() => _result.Value!.Model.SemanticVersion.IsAtLeast(SemanticVersion.V34).ShouldBeFalse();
    [Fact] void should_not_write_invocations() => System.Text.Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_result.Value!.Model)).ShouldNotContain("thenInvocations");
}
