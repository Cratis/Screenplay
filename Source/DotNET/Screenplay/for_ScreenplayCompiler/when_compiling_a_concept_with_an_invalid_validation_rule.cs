// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_concept_with_an_invalid_validation_rule : given.a_compiler
{
    const string Source =
        """
        concept EmailAddress : String @pii
          validate
            wibble 42
            not empty  message "Email is required"
          validate typescript
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_invalid_rule() => _result.Diagnostics.First().Message.ShouldEqual("Invalid validation rule 'wibble 42'");
    [Fact] void should_report_the_invalid_validate_declaration() => _result.Diagnostics.Last().Message.ShouldEqual("Invalid validate declaration 'validate typescript' - expected 'validate' or 'validate csharp'");
    [Fact] void should_report_errors_only() => _result.Diagnostics.All(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_keep_the_valid_rule() => _result.Value!.Concepts.Single().Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules.Single().Rule.ShouldEqual(ValidationRuleKind.NotEmpty);
}
