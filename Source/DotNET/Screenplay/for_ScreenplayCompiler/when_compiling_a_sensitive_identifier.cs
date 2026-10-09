// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_sensitive_identifier : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("""
        concept SecretId : Uuid secret
        module Secrets
          feature Registration
            slice StateChange Register
              command Register
                id SecretId identifier
        """);

    [Fact] void should_reject_the_model() => _result.Success.ShouldBeFalse();
    [Fact] void should_use_a_stable_error() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.PiiNotSupportedOnIdentifier);
    [Fact] void should_report_error_severity() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_locate_the_identifier() => _result.Diagnostics.Single().Location.Line.ShouldEqual(6);
    [Fact] void should_explain_the_surrogate_remedy() => _result.Diagnostics.Single().Message.ShouldEqual("Concept 'SecretId' is secret and cannot be an event source identifier - use a surrogate Uuid identifier and keep the secret value as a property");
}
