// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_scoped_command_examples : given.a_semantic_binder
{
    const string Source = """
        module M
          feature F
            slice StateChange A
              command Record
                id String identifier
                returns id
              example First : Record
                id = "first"
                for "first"
              specification FirstAction
                when First
                then returns "first"
            slice StateChange B
              command Record
                id Uuid identifier
                returns id
              example Second : Record
                id = "11111111-1111-1111-1111-111111111111"
                for "11111111-1111-1111-1111-111111111111"
              specification SecondAction
                when Second
                then returns "11111111-1111-1111-1111-111111111111"
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_admit_distinct_slice_local_destinations() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_keep_the_specification_origins() => _result.Value!.SpecificationOrigins.Count.ShouldEqual(2);

    [Theory]
    [InlineData("First", "M.F.A.Record")]
    [InlineData("Second", "F.B.Record")]
    void should_bind_a_qualified_command_in_its_own_slice(string example, string command) => Bind(Source.Replace($"when {example}", $"when {command} id = \"{(example == "First" ? "first" : "11111111-1111-1111-1111-111111111111")}\"", StringComparison.Ordinal)).Success.ShouldBeTrue();

    [Theory]
    [InlineData("M.F.A.Record")]
    [InlineData("M.F.A.First")]
    void should_not_silently_bind_another_slices_same_named_command(string reference)
    {
        var result = Bind(Source.Replace("when Second", $"when {reference}\n          id = \"11111111-1111-1111-1111-111111111111\"", StringComparison.Ordinal));
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("M.F.A.Record", StringComparison.Ordinal) && diagnostic.Message.Contains("unresolved in its slice", StringComparison.Ordinal));
    }
}
