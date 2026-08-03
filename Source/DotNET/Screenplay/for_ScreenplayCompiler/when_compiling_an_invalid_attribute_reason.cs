// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_invalid_attribute_reason : given.a_compiler
{
    const string Source =
        """
        concept BankAccount : String @pii
          sensitive reason "The concept never declared @sensitive"

        concept EmailAddress : String @pii
          pii reason "Billing contact address"
          pii reason "A second reason for the same attribute"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_missing_attribute() => _result.Diagnostics.First().Message.ShouldEqual("Concept 'BankAccount' declares a reason for 'sensitive' without the attribute - write 'concept BankAccount : <Type> @sensitive'");
    [Fact] void should_report_the_duplicate_reason() => _result.Diagnostics.Last().Message.ShouldEqual("Concept 'EmailAddress' already declares a reason for 'pii' - at most one is allowed");
    [Fact] void should_report_errors_only() => _result.Diagnostics.All(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_keep_the_first_reason() => _result.Value!.Concepts.Last().Attributes.Single().Reason.ShouldEqual("Billing contact address");
}
