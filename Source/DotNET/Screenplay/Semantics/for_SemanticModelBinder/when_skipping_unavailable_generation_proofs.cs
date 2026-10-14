// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_skipping_unavailable_generation_proofs : given.a_semantic_binder
{
    [Theory]
    [InlineData("pattern \"ORD-#{7}\"")]
    [InlineData("sequence")]
    [InlineData("rule Make")]
    void should_not_treat_a_refused_generation_as_an_implicit_uuid(string strategy)
    {
        var result = Bind($"concept Code : String\n  generate {strategy}\n  validate\n    matches \"^ORD-[0-9]{{7}}$\"");
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.RefutedGeneratedConceptRule || diagnostic.Code == DiagnosticCodes.UnprovenGeneratedConceptRule);
    }

    [Fact]
    void should_preserve_default_uuid_omission_for_an_unused_concept_with_rules()
    {
        var result = Bind("concept Id : Uuid\n  generate uuid\n  validate\n    rule Check\n      file Rules/Check.cs");
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V3);
        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.RefutedGeneratedConceptRule || diagnostic.Code == DiagnosticCodes.UnprovenGeneratedConceptRule);
    }
}
