// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_parsing_invalid_validation_severities : given.a_printer
{
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(
        """
        concept Label : String
          validate
            not empty severity critical message "Required"
        module Sales
          feature Orders
            slice StateChange Submit
              command Submit
                label Label
                validate
                  require label == "yes"
                    severity critical
                    severity warning
        """);

    [Fact] void should_report_the_invalid_rule_level() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.InvalidValidationSeverity).ShouldBeTrue();
    [Fact] void should_report_the_invalid_requirement_level() => _result.Diagnostics.Count(_ => _.Code == DiagnosticCodes.InvalidRequirementSeverity).ShouldEqual(2);
}
