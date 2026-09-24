// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_requirements_with_localized_messages : given.a_compiler
{
    const string Source =
        """
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                quantity Int
                validate
                  require quantity > 0
                    message $strings.orders.positiveQuantity
                  require quantity < 10
                    message "Too many"
                    severity warning
                  require quantity != 5
                    severity information
                    message $strings.orders.notFive
                  require quantity != 6
                    severity error
                    message "Not six"
        """;

    CompilationResult<ApplicationSyntax> _result;
    RequirementSyntax[] _requirements;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _requirements = [.. _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single()
            .Validations.OfType<DeclarativeValidateSyntax>().Single().Requirements!];
    }

    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_unquoted_key() => _requirements[0].Message.ShouldEqual("$strings.orders.positiveQuantity");
    [Fact] void should_keep_quoted_text() => _requirements[1].Message.ShouldEqual("Too many");
    [Fact] void should_keep_a_key_after_severity() => _requirements[2].Message.ShouldEqual("$strings.orders.notFive");
    [Fact] void should_keep_severity_after_a_quoted_message() => _requirements[1].Severity.ShouldEqual(ValidationSeverity.Warning);
    [Fact] void should_keep_severity_before_a_key() => _requirements[2].Severity.ShouldEqual(ValidationSeverity.Information);
    [Fact] void should_keep_explicit_error_severity() => _requirements[3].Severity.ShouldEqual(ValidationSeverity.Error);
}
