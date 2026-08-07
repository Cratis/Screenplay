// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_requirement_over_state_it_does_not_read : given.a_compiler
{
    const string Source =
        """
        module Timesheets
          feature HourRegistration
            slice StateChange StartMonth
              command StartMonth
                engagementId Uuid

                validate
                  require EngagementScope.isStarted == false
                    message "The month is already started"
                  require consultantId != "unassigned"
                    message "The engagement needs a consultant"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed_with_warnings() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_a_warning_per_unresolvable_operand() => _result.Diagnostics.Count().ShouldEqual(2);

    // Qualified by a read model the command never declared it reads.
    [Fact] void should_report_the_unread_source() =>
        _result.Diagnostics.First().Code.ShouldEqual(DiagnosticCodes.UnknownRequirementOperandSource);

    // Unqualified, and not one of the command's own properties either.
    [Fact] void should_report_the_operand_that_names_nothing() =>
        _result.Diagnostics.Last().Code.ShouldEqual(DiagnosticCodes.UnknownRequirementOperand);

    [Fact] void should_still_parse_both_requirements() => Requirements.Count().ShouldEqual(2);

    IEnumerable<RequirementSyntax> Requirements =>
        _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single()
            .Validations.OfType<DeclarativeValidateSyntax>().Single().Requirements!;
}
