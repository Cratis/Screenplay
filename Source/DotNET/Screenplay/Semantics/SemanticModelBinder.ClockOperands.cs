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
        // The clock candidate (see ClockOperandsBinding) admits these; the public binding keeps refusing them until admission.
        internal bool AllowClockOperands { get; init; }

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
            if (!AllowClockOperands)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "The application time zone is not yet admitted (decision 0106/0107) (#824).", zone.Location);
                return null;
            }

            if (!KnownTimeZone(zone.Id, "application time zone", zone.Location)) return null;
            UsesClock = true;
            return new SemanticTimeZone(zone.Id, zone.Configurable);
        }

        void RefuseCommandCalendar(CommandSyntax command)
        {
            if (command.Calendar is null || AllowClockOperands) return;
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Command '{command.Name}' declares a calendar, which is not yet admitted (decision 0106/0107) (#824).", command.Calendar.Location);
        }

        SemanticCommandCalendar? BindCommandCalendar(CommandSyntax command)
        {
            if (!AllowClockOperands || command.Calendar is not { } calendar) return null;
            if (calendar.ContextMember is { } member)
            {
                if (member == SemanticTypedContextCatalog.CalendarContextMember)
                {
                    UsesClock = true;
                    return new SemanticCommandCalendar(null, member);
                }

                Error(DiagnosticCodes.InvalidCalendarContextMember, $"Command '{command.Name}' reads its calendar from '$context.{member}'; the only trusted member is '$context.{SemanticTypedContextCatalog.CalendarContextMember}'.", calendar.Location);
                return null;
            }

            if (!KnownTimeZone(calendar.Id, $"calendar of command '{command.Name}'", calendar.Location)) return null;
            UsesClock = true;
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
            if (specification.GivenTimeZone is { } zone)
            {
                if (!AllowClockOperands)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Specification '{specification.Name}' states 'given time zone', which is not yet admitted (decision 0106/0107) (#824).", zone.Location);
                    return false;
                }

                if (!KnownTimeZone(zone.Id, $"'given time zone' of specification '{specification.Name}'", zone.Location)) return false;
                UsesClock = true;
            }

            return ValidateNoEventsExpectation(specification);
        }

        void ValidateClockOperandAdmission()
        {
            var operands = new ClockOperands { Candidate = AllowClockOperands };
            operands.VisitApplication(syntax);
            foreach (var operand in operands.Nodes)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Clock operands (today, now and date of) are not yet admitted (decision 0106/0107) (#824).", operand.Location);
            }
        }
    }

    // Check all authoring positions, including guarded screen actions and reactions, not just command conditions.
    private sealed class ClockOperands : ScreenplaySyntaxWalker
    {
        public List<ExpressionSyntax> Nodes { get; } = [];

        // The candidate binds commands itself (their require, produces-when and validation-rule positions); everything else stays refused.
        public bool Candidate { get; init; }

        // Clock operands are parsed in conditions, not property declarations or implementation files.
        // Avoid rescanning command properties and leave malformed implementation validation to the binder.
        public override void VisitCommand(CommandSyntax syntax)
        {
            if (Candidate) return;
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
