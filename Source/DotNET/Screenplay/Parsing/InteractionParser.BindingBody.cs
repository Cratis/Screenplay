// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class InteractionParser
{
    sealed class BindingBody(ParserContext context, SourceLine line, InteractionTriggerSyntax trigger)
    {
        readonly Dictionary<string, SourceLocation> _directiveLocations = [];
        readonly List<InteractionActionSyntax> _actions = [];
        readonly List<InteractionAlternativeSyntax> _alternatives = [];
        string? _condition;
        InteractionOtherwiseSyntax? _otherwise;
        bool _sawOtherwise;
        bool _sawAlternative;

        /// <summary>
        /// Parses the directives following the consumed interaction trigger.
        /// </summary>
        /// <returns>The binding, retaining recovered actions and alternatives.</returns>
        public InteractionBindingSyntax Parse()
        {
            while (context.TryPeekChild(line.Indent, out var child))
            {
                context.Reader.TakeSignificant();
                ParseDirective(child);
            }

            ValidateActions();
            ReportDeprecatedGuard();
            return new(trigger, _condition, _actions, line.Location)
            {
                DirectiveLocations = _directiveLocations,
                Alternatives = _alternatives,
                Otherwise = _otherwise
            };
        }

        void ParseDirective(SourceLine child)
        {
            switch (LineText.FirstWord(child.Content))
            {
                case "when":
                    ParseWhen(child);
                    return;
                case "otherwise":
                    ParseOtherwise(child);
                    return;
            }

            var where = WhereRegex().Match(child.Content);
            if (where.Success)
            {
                ParseGuard(child, where.Groups[1].Value.Trim());
                return;
            }

            if (ParseAction(context, child, 1) is { } action) _actions.Add(action);
        }

        void ParseWhen(SourceLine child)
        {
            _sawAlternative = true;
            if (_sawOtherwise)
            {
                context.Error(DiagnosticCodes.MisplacedActionOtherwise, "A 'when' alternative must precede 'otherwise'", child.Location);
            }

            if (ParseAlternative(context, child) is { } alternative) _alternatives.Add(alternative);
        }

        void ParseOtherwise(SourceLine child)
        {
            if (_sawOtherwise || child.Content != "otherwise")
            {
                context.Error(DiagnosticCodes.MisplacedActionOtherwise, "An interaction permits one final block-form 'otherwise'", child.Location);
            }

            _sawOtherwise = true;
            _otherwise = new(ParseAlternativeActions(context, child), child.Location);
        }

        void ParseGuard(SourceLine child, string condition)
        {
            if (_condition is not null)
            {
                context.Error(DiagnosticCodes.RepeatedInteractionCondition, "The binding already declares a 'where' guard - a binding has at most one", child.Location);
                return;
            }

            _condition = condition;
            _directiveLocations["where"] = child.Location;
        }

        void ValidateActions()
        {
            if (_sawAlternative || _sawOtherwise)
            {
                ValidateAlternatives();
                return;
            }

            if (_actions.Count == 0)
            {
                context.Error(DiagnosticCodes.InteractionBindingWithoutActions, $"'{line.Content}' declares no actions - a trigger with nothing to do is never what was meant", line.Location);
            }
        }

        void ValidateAlternatives()
        {
            if (!SupportsAlternatives(trigger))
            {
                context.Error(DiagnosticCodes.UnsupportedInteractionAlternatives, "Interaction alternatives are only supported on click, double click, select and change", line.Location);
            }

            if (_actions.Count > 0 || _condition is not null)
            {
                context.Error(DiagnosticCodes.MixedInteractionAlternatives, "Interaction alternatives cannot mix with plain actions or 'where'", line.Location);
            }

            if (!_sawAlternative)
            {
                context.Error(DiagnosticCodes.GuardedActionWithoutAlternatives, "A guarded interaction requires at least one 'when' alternative", line.Location);
            }
        }

        void ReportDeprecatedGuard()
        {
            if (_condition is null || !DeprecatesWhere(trigger)) return;
            var location = _directiveLocations["where"];
            const string message = "Opaque 'where' on click, double click and select is deprecated; use block-form 'when' item conditions";
            if (ParseStrictItemCondition(_condition, location, context.SourceOptions) is not null)
            {
                context.Warning(DiagnosticCodes.LegacyInteractionWhere, message, location);
            }
            else
            {
                context.Add(new(DiagnosticSeverity.Information, DiagnosticCodes.LegacyInteractionWhere, message, location));
            }
        }
    }
}
