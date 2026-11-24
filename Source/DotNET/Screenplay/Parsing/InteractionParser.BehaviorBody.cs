// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class InteractionParser
{
    sealed class BehaviorBody(ParserContext context, SourceLine header, string name)
    {
        readonly List<BehaviorParameterSyntax> _parameters = [];
        readonly List<InteractionBindingSyntax> _bindings = [];
        readonly Dictionary<string, SourceLocation> _directiveLocations = [];
        string? _description;
        int? _order;
        FileReferenceSyntax? _file;

        /// <summary>
        /// Parses the directives following the consumed behavior header.
        /// </summary>
        /// <returns>The behavior with its metadata and bindings in declaration order.</returns>
        public BehaviorSyntax Parse()
        {
            while (context.TryPeekChild(header.Indent, out var line))
            {
                context.Reader.TakeSignificant();
                ParseDirective(line);
            }

            if (_bindings.Count == 0)
            {
                context.Warning(
                    DiagnosticCodes.BehaviorWithoutBindings,
                    $"Behavior '{name}' declares no bindings - nothing it is attached to will ever run anything",
                    header.Location);
            }

            return new(name, _parameters, _bindings, header.Location, _description, _order)
            {
                File = _file,
                DirectiveLocations = _directiveLocations
            };
        }

        void ParseDirective(SourceLine line)
        {
            if (FileReferenceParser.IsDirective(line))
            {
                _file = FileReferenceParser.ParseReplacing(context, line, _file, _directiveLocations);
                return;
            }

            switch (LineText.FirstWord(line.Content))
            {
                case "description":
                    ParseDescription(line);
                    break;
                case "parameter":
                    ParseParameter(context, line, _parameters);
                    break;
                case "order":
                    ParseOrderDirective(line);
                    break;
                case "on":
                    if (ParseBinding(context, line) is { } binding) _bindings.Add(binding);
                    break;
                default:
                    context.Error(
                        DiagnosticCodes.UnknownBehaviorDirective,
                        $"Unexpected '{line.Content}' in behavior body - expected description, parameter, order or 'on <trigger>'",
                        line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        void ParseDescription(SourceLine line)
        {
            var previousDescription = _description;
            _description = DescriptionParser.Parse(context, line, _description, $"behavior '{name}'");
            if (previousDescription is null && _description is not null)
            {
                _directiveLocations["description"] = line.Location;
            }
        }

        void ParseOrderDirective(SourceLine line)
        {
            if (ParseOrder(context, line) is not { } parsedOrder) return;
            _order = parsedOrder;
            if (_directiveLocations.TryGetValue("order", out var previousOrder))
            {
                _directiveLocations[$"omitted:order:{previousOrder.Line}"] = previousOrder;
            }

            _directiveLocations["order"] = line.Location;
        }
    }
}
