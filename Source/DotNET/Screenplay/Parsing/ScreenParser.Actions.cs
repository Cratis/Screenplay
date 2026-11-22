// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class ScreenParser
{
    static ScreenActionSyntax? ParsePlainAction(ParserContext context, SourceLine line, (string Header, string? Label, ScreenActionSubjectKind? Subject) header)
    {
        var match = ActionRegex().Match(header.Header);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidActionDirective, $"Invalid action directive '{line.Content}' - expected 'action <Command>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var body = new PlainActionBody(header.Label, line.Location);
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            body.Read(context, child);
        }

        var action = body.Build(match.Groups[1].Value, header.Subject, line.Location);
        if (ScreenActionPresentation.InvalidReason(action) is { } reason)
        {
            context.Error(DiagnosticCodes.InvalidActionPresentation, reason, line.Location);
        }

        return action;
    }

    sealed class PlainActionBody(string? label, SourceLocation location)
    {
        readonly List<InteractionArgumentSyntax> _inputs = [];
        readonly List<PresentationValueSyntax> _presentation = [];
        string? _label = label;
        SourceLocation? _labelLocation = label is null ? null : location;
        ScreenNavigateSyntax? _navigate;

        internal void Read(ParserContext context, SourceLine child)
        {
            var labelMatch = LabelRegex().Match(child.Content);
            if (labelMatch.Success)
            {
                _label = OperandText(labelMatch, 1);
                _labelLocation = child.Location;
                return;
            }

            ReadDirective(context, child);
        }

        internal ScreenActionSyntax Build(string command, ScreenActionSubjectKind? subject, SourceLocation location) => new(command, _label, _navigate, location)
        {
            Subject = subject,
            Inputs = _inputs,
            Presentation = _presentation,
            DirectiveLocations = _labelLocation is null ? [] : new Dictionary<string, SourceLocation> { ["label"] = _labelLocation }
        };

        void ReadDirective(ParserContext context, SourceLine child)
        {
            switch (LineText.FirstWord(child.Content))
            {
                case "navigate":
                    _navigate = ParseNavigate(context, child.Content, child);
                    break;
                case "with":
                    if (ParsePlainActionInput(context, child, _inputs) is { } input) _inputs.Add(input);
                    break;
                case "presentation":
                    if (ParsePresentation(context, child) is { } value) _presentation.Add(value);
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownActionDirective, $"Unexpected '{child.Content}' in action - expected 'label \"...\"' or 'navigate to ...'", child.Location);
                    break;
            }
        }
    }
}
