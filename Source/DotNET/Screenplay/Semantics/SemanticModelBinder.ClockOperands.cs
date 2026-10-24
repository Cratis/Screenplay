// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        ComparisonConditionSyntax? AdmitClockFreeComparison(ConditionSyntax condition)
        {
            if (condition is not ComparisonConditionSyntax comparison)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "This condition has no portable ESM v1 meaning.", condition.Location);
                return null;
            }

            // The admission walker already reports the clock refusal, so do not bind it as a path.
            return comparison.LeftExpression is not null || comparison.Right is ClockOffsetExpressionSyntax or DateOfExpressionSyntax ? null : comparison;
        }

        void RefuseApplicationTimeZone()
        {
            if (syntax.TimeZone is null) return;
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, "The application time zone is not yet admitted (decision 0106/0107).", syntax.TimeZone.Location);
        }

        void RefuseCommandCalendar(CommandSyntax command)
        {
            if (command.Calendar is null) return;
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Command '{command.Name}' declares a calendar, which is not yet admitted (decision 0106/0107).", command.Calendar.Location);
        }

        bool ValidateClockSpecificationAdmission(SpecificationSyntax specification)
        {
            if (specification.GivenTimeZone is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Specification '{specification.Name}' states 'given time zone', which is not yet admitted (decision 0106/0107).", specification.GivenTimeZone.Location);
                return false;
            }

            return ValidateNoEventsExpectation(specification);
        }

        void ValidateClockOperandAdmission()
        {
            var operands = new ClockOperands();
            operands.VisitApplication(syntax);
            foreach (var operand in operands.Nodes)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Clock operands (today, now and date of) are not yet admitted (decision 0106/0107).", operand.Location);
            }
        }
    }

    // Check all authoring positions, including guarded screen actions and reactions, not just command conditions.
    private sealed class ClockOperands : ScreenplaySyntaxWalker
    {
        public List<ExpressionSyntax> Nodes { get; } = [];

        // Clock operands are parsed in conditions, not property declarations or implementation files.
        // Avoid rescanning command properties and leave malformed implementation validation to the binder.
        public override void VisitCommand(CommandSyntax syntax)
        {
            foreach (var validation in syntax.Validations) VisitValidate(validation);
            foreach (var production in syntax.Produces) VisitProduces(production);
        }

        public override void VisitImplementation(ImplementationSyntax syntax)
        {
        }

        public override void VisitClockOffsetExpression(ClockOffsetExpressionSyntax syntax) => Nodes.Add(syntax);

        public override void VisitDateOfExpression(DateOfExpressionSyntax syntax) => Nodes.Add(syntax);
    }
}
