// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_compliance_with_duplicate_declarations : given.a_semantic_binder
{
    [Theory]
    [InlineData("concept Protected : String pii\nconcept Protected : String pii", "Protected")]
    [InlineData("concept Protected : String\nconcept Protected : String pii", "Protected")]
    [InlineData("concept Protected : String\nconcept Protected : String secret\n  secret scope subject", "Protected")]
    [InlineData("concept Protected : String pii\ntype Details\n  value String\ntype Details\n  value Protected", "Details[]")]
    [InlineData("concept Protected : String pii\ntype Details\n  nested Details\ntype Details\n  value Protected", "Details")]
    void should_report_the_subject_refusal_without_throwing_for_ambiguous_types(string declarations, string type)
    {
        var source = $"{declarations}\nmodule M\n  feature F\n    slice StateChange S\n      event Changed\n        id Uuid subject\n        value {type}";
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("subject lineage", StringComparison.Ordinal));
    }
}
