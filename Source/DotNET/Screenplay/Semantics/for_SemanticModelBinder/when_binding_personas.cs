// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_personas : given.a_semantic_binder
{
    const string Source =
        """
        policy IsAccountant
          require role "accountant"
        module Invoicing
          feature Invoices
        """;

    CompilationResult<SemanticCompilation> _baseline;
    CompilationResult<SemanticCompilation> _withPersona;
    CompilationResult<SemanticCompilation> _unknownPolicy;

    void Because()
    {
        _baseline = Bind(Source);
        _withPersona = Bind(Source + "\npersona Accountant\n  policy IsAccountant\n");
        _unknownPolicy = Bind(Source + "\npersona Accountant\n  policy Missing\n");
    }

    [Fact] void should_bind_a_persona_with_a_known_policy() => _withPersona.Success.ShouldBeTrue();
    [Fact] void should_report_the_persona_as_information() => _withPersona.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.ReportOnlySemanticSyntax);
    [Fact] void should_keep_the_model_bytes_identical() => SemanticModelSerializer.Serialize(_withPersona.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_baseline.Value!.Model)).ShouldBeTrue();
    [Fact] void should_reject_a_persona_with_an_unknown_policy() => _unknownPolicy.Success.ShouldBeFalse();
    [Fact] void should_report_the_unknown_policy_as_an_error() => _unknownPolicy.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownPolicy).Severity.ShouldEqual(DiagnosticSeverity.Error);
}
