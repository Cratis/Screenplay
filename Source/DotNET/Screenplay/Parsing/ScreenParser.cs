// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>screen</c> declarations - intent level directives, the template whose slots they fill, and inline code.
/// </summary>
internal static partial class ScreenParser
{
    /// <summary>
    /// Parses a screen from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>screen</c> header.</param>
    /// <returns>The parsed <see cref="ScreenSyntax"/>.</returns>
    public static ScreenSyntax Parse(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content);
        if (!name.Success)
        {
            context.Error(DiagnosticCodes.InvalidScreenDeclaration, $"Invalid screen declaration '{header.Content}' - expected 'screen <Name>'", header.Location);
        }

        FileReferenceSyntax? file = null;
        var directiveLocations = new Dictionary<string, SourceLocation>();
        var directives = new List<ScreenDirectiveSyntax>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (FileReferenceParser.IsDirective(line))
            {
                file = FileReferenceParser.ParseReplacing(context, line, file, directiveLocations);
            }
            else if (ParseDirective(context, line) is { } directive)
            {
                directives.Add(directive);
            }
        }

        return new(name.Groups[1].Value, file, directives, header.Location) { DirectiveLocations = directiveLocations };
    }

    static ScreenDirectiveSyntax? ParseDirective(ParserContext context, SourceLine line)
    {
        switch (LineText.FirstWord(line.Content))
        {
            case "data":
                return ParseData(context, line);
            case "action":
                return ParseAction(context, line);
            case "template":
                return ParseTemplateReference(context, line);
            case "section":
                return ParseSection(context, line);
            case "title":
                return ParseTitle(context, line);
            case "table":
                return ParseTable(context, line);
            case "summary":
                return ParseSummary(context, line);
            case "component":
                return ParseComponent(context, line);
            case "toolbar":
                return ParseToolbar(context, line);
            case "navigate":
                return ParseNavigate(context, line.Content, line);
            case "on":
                return InteractionParser.ParseInlineBehavior(context, line) is { } behavior
                    ? new ScreenBehaviorSyntax(behavior, line.Location)
                    : null;
            case "uses":
                return InteractionParser.ParseUses(context, line) is { } uses
                    ? new ScreenUsesBehaviorSyntax(uses, line.Location)
                    : null;
            default:
                if (CodeBlockParser.IsCodeLine(context, line))
                {
                    var code = CodeBlockParser.Parse(context, line);
                    return code is null ? null : new ScreenCodeSyntax(code, line.Location);
                }

                context.Error(DiagnosticCodes.UnknownScreenDirective, $"Unexpected '{line.Content}' in screen body", line.Location);
                context.SkipBlock(line.Indent);
                return null;
        }
    }

    static ScreenDataSyntax? ParseData(ParserContext context, SourceLine line)
    {
        var match = DataRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidDataDirective, $"Invalid data directive '{line.Content}' - expected 'data <ReadModel> via query <Query> [by <param>]'", line.Location);
            return null;
        }

        var type = PropertyLineParser.ParseTypeRef(match.Groups[1].Value, line.Location);
        return new(type, match.Groups[2].Value, match.Groups[3].Success ? match.Groups[3].Value : null, line.Location);
    }

    static ScreenDirectiveSyntax? ParseAction(ParserContext context, SourceLine line)
    {
        var guarded = GuardedActionRegex().Match(line.Content);
        if (guarded.Success)
        {
            return ParseGuardedAction(context, line, OperandText(guarded, 1));
        }

        var match = ActionRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidActionDirective, $"Invalid action directive '{line.Content}' - expected 'action <Command>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        string? label = null;
        SourceLocation? labelLocation = null;
        ScreenNavigateSyntax? navigate = null;

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var labelMatch = LabelRegex().Match(child.Content);
            if (labelMatch.Success)
            {
                label = OperandText(labelMatch, 1);
                labelLocation = child.Location;
            }
            else if (LineText.FirstWord(child.Content) == "navigate")
            {
                navigate = ParseNavigate(context, child.Content, child);
            }
            else
            {
                context.Error(DiagnosticCodes.UnknownActionDirective, $"Unexpected '{child.Content}' in action - expected 'label \"...\"' or 'navigate to ...'", child.Location);
            }
        }

        return new ScreenActionSyntax(match.Groups[1].Value, label, navigate, line.Location)
        {
            DirectiveLocations = labelLocation is null ? [] : new Dictionary<string, SourceLocation> { ["label"] = labelLocation }
        };
    }

    static ScreenNavigateSyntax? ParseNavigate(ParserContext context, string text, SourceLine line)
    {
        var match = NavigateRegex().Match(text);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidNavigation, $"Invalid navigation '{text}' - expected 'navigate to <Screen> [by <param>]'", line.Location);
            return null;
        }

        var parameters = new List<ScreenNavigationParameterSyntax>();
        string? route = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var parameter = ParameterRegex().Match(child.Content);
            if (parameter.Success)
            {
                parameters.Add(new(parameter.Groups[1].Value, UiBindingParser.ParseFromClause(context, parameter.Groups[2].Value, child.Location), child.Location));
                continue;
            }

            var routeMatch = RouteRegex().Match(child.Content);
            if (routeMatch.Success)
            {
                route = OperandText(routeMatch, 1);
                continue;
            }

            context.Error(DiagnosticCodes.InvalidNavigation, $"Unexpected '{child.Content}' in navigation - expected 'route \"...\"' or 'parameter <name> from <binding>'", child.Location);
        }

        return new(match.Groups[1].Value, match.Groups[2].Success ? match.Groups[2].Value : null, line.Location) { Route = route, Parameters = parameters };
    }

    static ScreenTemplateReferenceSyntax ParseTemplateReference(ParserContext context, SourceLine line)
    {
        var name = line.Content["template".Length..].Trim();
        var slots = new List<ScreenSlotSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (!SlotRegex().IsMatch(child.Content))
            {
                context.Error(DiagnosticCodes.InvalidScreenLayoutSlot, $"Expected a slot name in template '{name}', got '{child.Content}'", child.Location);
                context.SkipBlock(child.Indent);
                continue;
            }

            var directives = new List<ScreenDirectiveSyntax>();
            while (context.TryPeekChild(child.Indent, out var slotChild))
            {
                context.Reader.TakeSignificant();
                if (ParseDirective(context, slotChild) is { } directive)
                {
                    directives.Add(directive);
                }
            }

            slots.Add(new(child.Content, directives, child.Location));
        }

        return new(name, slots, line.Location);
    }

    static ScreenSectionSyntax ParseSection(ParserContext context, SourceLine line)
    {
        var name = line.Content["section".Length..].Trim();
        var directives = new List<ScreenDirectiveSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (ParseDirective(context, child) is { } directive)
            {
                directives.Add(directive);
            }
        }

        return new ScreenSectionSyntax(name, directives, line.Location);
    }

    static ScreenTitleSyntax ParseTitle(ParserContext context, SourceLine line)
    {
        var match = TitleRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidTitleDirective, $"Invalid title directive '{line.Content}' - expected 'title \"...\"'", line.Location);
            return new(string.Empty, line.Location);
        }

        return new(OperandText(match, 1), line.Location);
    }

    static ScreenTableSyntax ParseTable(ParserContext context, SourceLine line)
    {
        var target = line.Content["table".Length..].Trim();
        var columns = new List<ScreenColumnSyntax>();
        var behaviors = new List<BehaviorSyntax>();
        var usedBehaviors = new List<UsesBehaviorSyntax>();
        ScreenNavigateSyntax? rowClick = null;

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var column = ColumnRegex().Match(child.Content);
            if (column.Success)
            {
                columns.Add(new(column.Groups[1].Value, column.Groups[2].Success || column.Groups[3].Success ? OperandText(column, 2) : null, child.Location));
                continue;
            }

            var click = RowClickRegex().Match(child.Content);
            if (click.Success)
            {
                rowClick = ParseNavigate(context, click.Groups[1].Value, child);
                continue;
            }

            // A table is the element interaction is most often attached to - a row opening a dialog, a
            // selection driving a detail pane. 'on row-click' above stays what it was; anything else starting
            // with 'on', plus 'uses', is a behavior attached to the table.
            if (InteractionParser.IsInteractionDirective(child))
            {
                if (LineText.FirstWord(child.Content) == "uses")
                {
                    if (InteractionParser.ParseUses(context, child) is { } tableUses)
                    {
                        usedBehaviors.Add(tableUses);
                    }
                }
                else if (InteractionParser.ParseInlineBehavior(context, child) is { } tableBehavior)
                {
                    behaviors.Add(tableBehavior);
                }

                continue;
            }

            context.Error(DiagnosticCodes.UnknownTableDirective, $"Unexpected '{child.Content}' in table - expected 'column ...', 'on row-click navigate to ...', 'on <trigger>' or 'uses <Behavior>'", child.Location);
        }

        return new(target, columns, rowClick, line.Location) { Behaviors = behaviors, UsedBehaviors = usedBehaviors };
    }

    static ScreenSummarySyntax ParseSummary(ParserContext context, SourceLine line)
    {
        var target = line.Content["summary".Length..].Trim();
        var fields = new List<ScreenFieldSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var field = FieldRegex().Match(child.Content);
            if (!field.Success)
            {
                context.Error(DiagnosticCodes.UnknownSummaryDirective, $"Unexpected '{child.Content}' in summary - expected 'field <property> label \"...\"'", child.Location);
                continue;
            }

            fields.Add(new(field.Groups[1].Value, OperandText(field, 2), child.Location));
        }

        return new(target, fields, line.Location);
    }


    static ScreenComponentSyntax? ParseComponent(ParserContext context, SourceLine line)
    {
        var match = ComponentRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.UnknownScreenDirective, $"Invalid component directive '{line.Content}' - expected 'component <Package.Component> <name>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        UiBindingSyntax? dataContext = null;
        string? icon = null;
        var properties = new List<ComponentPropertySyntax>();
        var exposes = new List<ComponentExposedValueSyntax>();
        var presentation = new List<PresentationValueSyntax>();
        var outlets = new List<ComponentOutletSyntax>();
        var behaviors = new List<BehaviorSyntax>();
        var usedBehaviors = new List<UsesBehaviorSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "context":
                    dataContext = UiBindingParser.Parse(context, child.Content["context".Length..].Trim(), child.Location);
                    break;
                case "property":
                    if (ParseComponentProperty(context, child) is { } property) properties.Add(property);
                    break;
                case "icon":
                    icon = child.Content["icon".Length..].Trim();
                    break;
                case "presentation":
                    if (ParsePresentation(context, child) is { } value) presentation.Add(value);
                    break;
                case "exposes":
                    if (ParseComponentExposes(context, child) is { } exposed) exposes.Add(exposed);
                    break;
                case "outlet":
                    outlets.Add(ParseComponentOutlet(context, child));
                    break;
                case "on":
                case "uses":
                    InteractionParser.ParseAttachment(context, child, behaviors, usedBehaviors);
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownScreenDirective, $"Unexpected '{child.Content}' in component - expected context, property, icon, presentation, exposes, outlet, on or uses", child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return new(match.Groups[1].Value, match.Groups[2].Value, line.Location)
        {
            Context = dataContext,
            Properties = properties,
            Exposes = exposes,
            Presentation = presentation,
            Icon = string.IsNullOrWhiteSpace(icon) ? null : icon,
            Outlets = outlets,
            Behaviors = behaviors,
            UsedBehaviors = usedBehaviors
        };
    }

    static ComponentPropertySyntax? ParseComponentProperty(ParserContext context, SourceLine line)
    {
        var bound = ComponentPropertyBindingRegex().Match(line.Content);
        if (bound.Success) return new(bound.Groups[1].Value, UiBindingParser.ParseFromClause(context, bound.Groups[2].Value, line.Location), null, line.Location);

        var literal = ComponentPropertyLiteralRegex().Match(line.Content);
        if (literal.Success) return new(literal.Groups[1].Value, null, OperandText(literal, 2), line.Location);

        context.Error(DiagnosticCodes.UnknownScreenDirective, $"Invalid component property '{line.Content}' - expected 'property <path> from <binding>' or 'property <path> = \"value\"'", line.Location);
        return null;
    }

    static ComponentExposedValueSyntax? ParseComponentExposes(ParserContext context, SourceLine line)
    {
        var match = ExposesRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.UnknownScreenDirective, $"Invalid exposed value '{line.Content}' - expected 'exposes <name> from <binding>'", line.Location);
            return null;
        }

        return new(match.Groups[1].Value, UiBindingParser.ParseFromClause(context, match.Groups[2].Value, line.Location), line.Location);
    }

    static ComponentOutletSyntax ParseComponentOutlet(ParserContext context, SourceLine line)
    {
        var name = line.Content["outlet".Length..].Trim();
        var directives = new List<ScreenDirectiveSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (ParseDirective(context, child) is { } directive) directives.Add(directive);
        }

        return new(name, directives, line.Location);
    }

    static ScreenToolbarSyntax ParseToolbar(ParserContext context, SourceLine line)
    {
        var name = line.Content["toolbar".Length..].Trim();
        var items = new List<ToolbarItemSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (ParseToolbarItem(context, child) is { } item) items.Add(item);
        }

        return new(name, items, line.Location);
    }

    static ToolbarItemSyntax? ParseToolbarItem(ParserContext context, SourceLine line)
    {
        var match = ToolbarItemRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.UnknownScreenDirective, $"Invalid toolbar item '{line.Content}' - expected 'item <name> action <Command>', 'item <name> navigate to <Screen>' or 'item <name> dialog <DialogTemplate>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var kind = match.Groups[2].Value switch
        {
            "action" => ToolbarItemKind.Action,
            "navigate" => ToolbarItemKind.Navigate,
            _ => ToolbarItemKind.Dialog
        };
        var item = new ToolbarItemSyntax(match.Groups[1].Value, kind, match.Groups[3].Value, line.Location);
        string? label = null;
        string? icon = null;
        var parameters = new List<ScreenNavigationParameterSyntax>();
        var presentation = new List<PresentationValueSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var labelMatch = LabelRegex().Match(child.Content);
            if (labelMatch.Success)
            {
                label = OperandText(labelMatch, 1);
                continue;
            }

            var parameter = ParameterRegex().Match(child.Content);
            if (parameter.Success)
            {
                parameters.Add(new(parameter.Groups[1].Value, UiBindingParser.ParseFromClause(context, parameter.Groups[2].Value, child.Location), child.Location));
                continue;
            }

            if (LineText.FirstWord(child.Content) == "icon")
            {
                icon = child.Content["icon".Length..].Trim();
                continue;
            }

            if (ParsePresentation(context, child) is { } value)
            {
                presentation.Add(value);
                continue;
            }

            context.Error(DiagnosticCodes.UnknownScreenDirective, $"Unexpected '{child.Content}' in toolbar item - expected label, icon, parameter or presentation", child.Location);
        }

        return item with { Label = label, Icon = icon, Parameters = parameters, Presentation = presentation };
    }

    static PresentationValueSyntax? ParsePresentation(ParserContext context, SourceLine line)
    {
        var match = PresentationRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.UnknownScreenDirective, $"Invalid presentation value '{line.Content}' - expected 'presentation <key> \"value\"'", line.Location);
            return null;
        }

        return new(match.Groups[1].Value, OperandText(match, 2), line.Location);
    }

    static string OperandText(Match match, int quotedGroup) =>
        match.Groups[quotedGroup].Success ? StringLiteral.Unescape(match.Groups[quotedGroup].Value) : match.Groups[quotedGroup + 1].Value;

    [GeneratedRegex(@"^screen\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^data\s+([\w.]+(?:\[\])?)\s+via\s+query\s+(\w+(?:\.\w+)*)(?:\s+by\s+(\w+))?$", RegexOptions.None, 1000)]
    private static partial Regex DataRegex();

    [GeneratedRegex(@"^action\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex ActionRegex();

    [GeneratedRegex("^label\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.None, 1000)]
    private static partial Regex LabelRegex();

    [GeneratedRegex(@"^navigate\s+to\s+(\w+(?:\.\w+)*)(?:\s+by\s+(\w+))?$", RegexOptions.None, 1000)]
    private static partial Regex NavigateRegex();

    [GeneratedRegex("^route\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\S+))$", RegexOptions.None, 1000)]
    private static partial Regex RouteRegex();

    [GeneratedRegex(@"^parameter\s+([A-Za-z_]\w*)\s+from\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex ParameterRegex();

    [GeneratedRegex(@"^component\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ComponentRegex();

    [GeneratedRegex(@"^property\s+([\w.]+)\s+from\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex ComponentPropertyBindingRegex();

    [GeneratedRegex("^property\\s+([\\w.]+)\\s*=\\s*(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\S+))$", RegexOptions.None, 1000)]
    private static partial Regex ComponentPropertyLiteralRegex();

    [GeneratedRegex(@"^exposes\s+([A-Za-z_]\w*)\s+from\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex ExposesRegex();

    [GeneratedRegex("^presentation\\s+([A-Za-z_]\\w*)\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\S+))$", RegexOptions.None, 1000)]
    private static partial Regex PresentationRegex();

    [GeneratedRegex(@"^item\s+([A-Za-z_]\w*)\s+(action|navigate|dialog)(?:\s+to)?\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex ToolbarItemRegex();

    [GeneratedRegex(@"^[a-z_]\w*$", RegexOptions.None, 1000)]
    private static partial Regex SlotRegex();

    [GeneratedRegex("^title\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.None, 1000)]
    private static partial Regex TitleRegex();

    [GeneratedRegex("^column\\s+([\\w.]+)(?:\\s+label\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*)))?$", RegexOptions.None, 1000)]
    private static partial Regex ColumnRegex();

    [GeneratedRegex(@"^on\s+row-click\s+(navigate\s+to\s+.+)$", RegexOptions.None, 1000)]
    private static partial Regex RowClickRegex();

    [GeneratedRegex("^field\\s+([\\w.]+)\\s+label\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.None, 1000)]
    private static partial Regex FieldRegex();
}
