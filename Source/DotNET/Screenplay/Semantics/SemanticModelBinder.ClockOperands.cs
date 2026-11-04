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

        SemanticTimeZone? BindApplicationTimeZone()
        {
            if (syntax.TimeZone is not { } zone) return null;
            if (!KnownTimeZone(zone.Id, "application time zone", zone.Location)) return null;
            return new SemanticTimeZone(zone.Id, zone.Configurable);
        }

        SemanticCommandCalendar? BindCommandCalendar(CommandSyntax command)
        {
            if (command.Calendar is not { } calendar) return null;
            if (calendar.ContextMember is { } member)
            {
                if (IsCalendarContextMember(member))
                {
                    return new SemanticCommandCalendar(null, member);
                }

                Error(DiagnosticCodes.InvalidCalendarContextMember, $"Command '{command.Name}' reads its calendar from '$context.{member}'; {TrustedCalendarMembers()}.", calendar.Location);
                return null;
            }

            if (!KnownTimeZone(calendar.Id, $"calendar of command '{command.Name}'", calendar.Location)) return null;
            return new SemanticCommandCalendar(calendar.Id, null);
        }

        bool KnownTimeZone(string? id, string subject, SourceLocation location)
        {
            if (IanaTimeZones.IsKnown(id)) return true;
            Error(DiagnosticCodes.UnknownTimeZone, $"The {subject} names '{id}', which is not a known IANA time zone. There is no fallback to UTC.", location);
            return false;
        }

        bool ValidateClockSpecificationAdmission(SpecificationSyntax specification)
        {
            if (specification.GivenTimeZone is { } zone && !KnownTimeZone(zone.Id, $"'given time zone' of specification '{specification.Name}'", zone.Location)) return false;

            return ValidateNoEventsExpectation(specification);
        }

        void ValidateClockOperandAdmission()
        {
            var operands = new ClockOperands();
            operands.VisitApplication(syntax);
            foreach (var operand in operands.Nodes)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Clock operands (today, now and date of) are not admitted in this position (decisions 0106 and 0107).", operand.Location);
            }
        }
    }

    // Check all authoring positions, including guarded screen actions and reactions, not just command conditions.
    private sealed class ClockOperands : ScreenplaySyntaxWalker
    {
        public List<ExpressionSyntax> Nodes { get; } = [];

        // Commands bind their own clock positions (require, produces when and validation rules); everything else stays refused.
        // Clock operands are parsed in conditions, not implementation files, so leave malformed implementation validation to the binder.
        public override void VisitCommand(CommandSyntax syntax)
        {
        }

        public override void VisitImplementation(ImplementationSyntax syntax)
        {
        }

        public override void VisitClockOffsetExpression(ClockOffsetExpressionSyntax syntax) => Nodes.Add(syntax);

        public override void VisitDateOfExpression(DateOfExpressionSyntax syntax) => Nodes.Add(syntax);
    }
}
