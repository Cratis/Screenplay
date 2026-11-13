// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of the <c>screen</c> construct - intent level directives, structural layouts and inline code.
/// </summary>
public partial class ScreenplayPrinter
{
    static bool HasScreenActionBody(ScreenActionSyntax action) => action.Label is not null || action.Navigate is not null || action.Inputs.Any();

    void WriteScreen(ScreenplayWriter writer, ScreenSyntax screen)
    {
        using var anchor = writer.Anchor(screen);
        writer.Line($"screen {screen.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, screen.Description, screen);
            WriteAvailable(writer, screen.Available);
            if (screen.File is not null)
            {
                writer.Line($"file {screen.File.Path}", screen.File);
            }

            if (screen.Fits.Any())
            {
                writer.Line($"fits {string.Join(", ", screen.Fits.Select(ScreenFitsParser.Keyword))}");
            }

            foreach (var visual in screen.Visuals)
            {
                writer.Line($"{ScreenVisualParser.Keyword(visual.Kind)} {visual.File.Path}", visual);
            }

            foreach (var directive in screen.Directives)
            {
                WriteScreenDirective(writer, directive);
            }

            WriteScreenContributions(writer, screen.Contributions);
            WriteOutlets(writer, screen.Outlets);
        }
    }

    void WriteScreenDirective(ScreenplayWriter writer, ScreenDirectiveSyntax directive)
    {
        using var anchor = writer.Anchor(directive);
        switch (directive)
        {
            case ScreenDataSyntax data:
                writer.Line(WriteScreenData(data));
                break;
            case ScreenActionSyntax action:
                WriteScreenAction(writer, action);
                break;
            case ScreenGuardedActionSyntax guarded:
                WriteScreenGuardedAction(writer, guarded);
                break;
            case ScreenNavigateSyntax navigate:
                writer.Line(WriteScreenNavigate(navigate));
                using (writer.Indent())
                {
                    WriteScreenNavigateBody(writer, navigate);
                }

                break;
            case ScreenTemplateReferenceSyntax template:
                WriteScreenTemplateReference(writer, template);
                break;
            case ScreenSectionSyntax section:
                WriteScreenSection(writer, section);
                break;
            case ScreenTitleSyntax title:
                writer.Line($"title {ScreenplaySyntaxText.LocalizableString(title.Text)}");
                break;
            case ScreenShowSyntax show:
                writer.Line($"show {show.Screen} by {show.By} from {show.Element}.selection");
                break;
            case ScreenTableSyntax table:
                WriteScreenTable(writer, table);
                break;
            case ScreenSummarySyntax summary:
                WriteScreenSummary(writer, summary);
                break;
            case ScreenComponentSyntax component:
                WriteScreenComponent(writer, component);
                break;
            case ScreenToolbarSyntax toolbar:
                WriteScreenToolbar(writer, toolbar);
                break;
            case ScreenCodeSyntax code:
                WriteCodeBlock(writer, code.Code);
                break;
            case ScreenBehaviorSyntax behavior:
                WriteAttachedBehavior(writer, behavior.Behavior);
                break;
            case ScreenUsesBehaviorSyntax uses:
                WriteUsesBehavior(writer, uses.Uses);
                break;
            default:
                throw new UnsupportedSyntaxForPrinting("screen directive", directive.GetType().Name);
        }
    }

    string WriteScreenData(ScreenDataSyntax data)
    {
        var head = $"data {ScreenplaySyntaxText.TypeRef(data.Type)} via query {data.Query}";
        return data.By is null ? head : $"{head} by {data.By}";
    }

    string WriteScreenNavigate(ScreenNavigateSyntax navigate)
    {
        var head = $"navigate to {navigate.Screen}";
        return navigate.By is null ? head : $"{head} by {navigate.By}";
    }

    void WriteScreenNavigateBody(ScreenplayWriter writer, ScreenNavigateSyntax navigate)
    {
        if (navigate.Route is not null)
        {
            writer.Line($"route {ScreenplaySyntaxText.LocalizableString(navigate.Route)}", navigate);
        }

        if (navigate.Slot is not null)
        {
            writer.Line($"slot {navigate.Slot}", navigate);
        }

        foreach (var parameter in navigate.Parameters)
        {
            WriteBindingLine(writer, $"parameter {parameter.Name}", parameter.Binding, parameter);
        }
    }

    // A structured literal binding ends its line with 'from literal' and its modifiers; the value follows as an indented block.
    void WriteBindingLine(ScreenplayWriter writer, string prefix, UiBindingSyntax binding, SyntaxNode node)
    {
        var text = $"{prefix} {WriteUiBinding(writer, binding)}";
        if (binding is { BindingKind: UiBindingKind.Literal, Literal: { } literal } && ScreenplaySyntaxText.IsStructured(literal))
        {
            using var anchor = writer.Anchor(node);
            writer.Structured(text, literal);
            return;
        }

        writer.Line(text, node);
    }

    string WriteUiBinding(ScreenplayWriter writer, UiBindingSyntax binding)
    {
        var head = binding.BindingKind switch
        {
            UiBindingKind.DataContext => $"from data {binding.Path}",
            UiBindingKind.QueryResult => string.IsNullOrWhiteSpace(binding.Path) ? $"from query {binding.Query}" : $"from query {binding.Query}.{binding.Path}",
            UiBindingKind.ComponentProperty => $"from component {WriteStableComponentId(binding.ComponentId)}.{binding.ComponentPropertyPath ?? binding.Path}",
            UiBindingKind.Literal when binding.Literal is not null && ScreenplaySyntaxText.IsStructured(binding.Literal) => "from literal",
            UiBindingKind.Literal => $"from literal {writer.Expression(binding.Literal ?? new LiteralExpressionSyntax(null, binding.Location))}",
            _ => binding.RawText ?? binding.Path
        };

        if (binding.Mode is not null)
        {
            head = $"{head} mode {(binding.Mode == UiBindingMode.TwoWay ? "twoWay" : "oneWay")}";
        }

        if (binding.NullBehavior is not null)
        {
            var nullBehavior = binding.NullBehavior switch
            {
                UiBindingNullBehavior.Clear => "clear",
                UiBindingNullBehavior.Preserve => "preserve",
                _ => "propagate"
            };
            head = $"{head} null {nullBehavior}";
        }

        if (binding.ExpectedValueType is not null)
        {
            head = $"{head} expected {binding.ExpectedValueType}";
        }

        return head;
    }

    void WriteScreenAction(ScreenplayWriter writer, ScreenActionSyntax action)
    {
        using var anchor = writer.Anchor(action);
        writer.Line($"action {action.Command}{WriteActionSubject(action.Subject)}");
        if (!HasScreenActionBody(action))
        {
            return;
        }

        using (writer.Indent())
        {
            if (action.Label is not null)
            {
                writer.DirectiveLine($"label {ScreenplaySyntaxText.LocalizableString(action.Label)}", action, "label");
            }

            foreach (var input in action.Inputs) writer.Line($"with {input.Name} from {input.Binding}", input);

            if (action.Navigate is not null)
            {
                writer.Line(WriteScreenNavigate(action.Navigate), action.Navigate);
                using (writer.Indent())
                {
                    WriteScreenNavigateBody(writer, action.Navigate);
                }
            }
        }
    }

    void WriteScreenTemplateReference(ScreenplayWriter writer, ScreenTemplateReferenceSyntax template)
    {
        using var anchor = writer.Anchor(template);
        writer.Line($"template {WriteTemplateReference(template.Blueprint, template.Name, template.Version)}");
        using (writer.Indent())
        {
            foreach (var slot in template.Slots)
            {
                writer.Line(slot.Name, slot);
                using (writer.Indent())
                {
                    foreach (var directive in slot.Directives)
                    {
                        WriteScreenDirective(writer, directive);
                    }
                }
            }
        }
    }

    void WriteScreenSection(ScreenplayWriter writer, ScreenSectionSyntax section)
    {
        using var anchor = writer.Anchor(section);
        writer.Line($"section {section.Name}");
        using (writer.Indent())
        {
            foreach (var directive in section.Directives)
            {
                WriteScreenDirective(writer, directive);
            }
        }
    }

    void WriteScreenTable(ScreenplayWriter writer, ScreenTableSyntax table)
    {
        using var anchor = writer.Anchor(table);
        writer.Line($"table {table.Target}");
        using (writer.Indent())
        {
            foreach (var column in table.Columns)
            {
                writer.Line(
                    column.Label is null
                        ? $"column {column.Property}"
                        : $"column {column.Property} label {ScreenplaySyntaxText.LocalizableString(column.Label)}",
                    column);
            }

            if (table.RowClick is not null)
            {
                writer.Line($"on row-click {WriteScreenNavigate(table.RowClick)}", table.RowClick);
                using (writer.Indent())
                {
                    WriteScreenNavigateBody(writer, table.RowClick);
                }
            }

            foreach (var behavior in table.Behaviors)
            {
                WriteAttachedBehavior(writer, behavior);
            }

            foreach (var uses in table.UsedBehaviors)
            {
                WriteUsesBehavior(writer, uses);
            }
        }
    }

    void WriteScreenComponent(ScreenplayWriter writer, ScreenComponentSyntax component)
    {
        using var anchor = writer.Anchor(component);
        writer.Line($"component {component.Component} {component.Name}");
        using (writer.Indent())
        {
            if (component.StableId is not null)
            {
                writer.Line($"id {ScreenplaySyntaxText.LocalizableString(component.StableId)}");
            }

            if (component.Context is not null)
            {
                writer.Line($"context {WriteUiBinding(writer, component.Context)}");
            }

            foreach (var property in component.Properties)
            {
                if (property.Binding is not null) WriteBindingLine(writer, $"property {property.Property}", property.Binding, property);
                else writer.Assignment($"property {property.Property}", property.Value ?? new LiteralExpressionSyntax(null, property.Location), property);
            }

            if (component.Icon is not null)
            {
                writer.Line($"icon {component.Icon}");
            }

            foreach (var value in component.Presentation)
            {
                writer.Line($"presentation {value.Name} {ScreenplaySyntaxText.LocalizableString(value.Value)}", value);
            }

            foreach (var exposed in component.Exposes)
            {
                WriteBindingLine(writer, $"exposes {exposed.Name}", exposed.Binding, exposed);
            }

            foreach (var slot in component.Slots)
            {
                writer.Line($"slot {slot.Name}", slot);
                using (writer.Indent())
                {
                    foreach (var directive in slot.Directives)
                    {
                        WriteScreenDirective(writer, directive);
                    }
                }
            }

            WriteAttachments(writer, component.Behaviors, component.UsedBehaviors);
        }
    }

    string WriteStableComponentId(string? componentId)
    {
        if (string.IsNullOrWhiteSpace(componentId))
        {
            return string.Empty;
        }

        return IsIdentifier(componentId) ? componentId : ScreenplaySyntaxText.LocalizableString(componentId);
    }

    bool IsIdentifier(string text) =>
        text.Length > 0 &&
        (char.IsLetter(text[0]) || text[0] == '_') &&
        text.All(character => char.IsLetterOrDigit(character) || character == '_');

    void WriteScreenToolbar(ScreenplayWriter writer, ScreenToolbarSyntax toolbar)
    {
        using var anchor = writer.Anchor(toolbar);
        writer.Line($"toolbar {toolbar.Name}");
        using (writer.Indent())
        {
            foreach (var item in toolbar.Items)
            {
                WriteToolbarItem(writer, item);
            }

            foreach (var button in toolbar.Buttons)
            {
                WriteToolbarButton(writer, button);
            }
        }
    }

    void WriteToolbarButton(ScreenplayWriter writer, ToolbarButtonSyntax button)
    {
        var line = $"button {WriteItemId(button.Id)} title {ScreenplaySyntaxText.LocalizableString(button.Title)}";
        if (button.Slice is not null) line += $" slice {button.Slice}";
        if (button.Emphasis is { } emphasis) line += $" emphasis {emphasis.ToString().ToLowerInvariant()}";
        if (button.Icon is not null) line += $" icon {WriteItemValue(button.Icon)}";
        if (button.Runs is not null) line += $" runs {button.Runs}";
        line += WriteActionSubject(button.Subject);
        line += button.Opens switch
        {
            HookOpening.Dialog => " opens dialog",
            HookOpening.Drawer => " opens drawer",
            HookOpening.Slot => $" opens in slot {button.OpensSlot}",
            _ => string.Empty
        };
        writer.Line(ItemHead(button, line), button);
        if (button.Authorize is not null || button.Confirm is not null || button.Position is not null || button.Opens == HookOpening.Runs || button.EnabledWhen is not null)
        {
            using (writer.Indent())
            {
                if (button.Opens == HookOpening.Runs)
                {
                    writer.Line("runs");
                }

                if (button.EnabledWhen is not null)
                {
                    writer.Line($"enabled when {ScreenplaySyntaxText.Condition(button.EnabledWhen)}");
                }

                if (button.Confirm is not null)
                {
                    writer.Line($"confirm {ScreenplaySyntaxText.LocalizableString(button.Confirm)}");
                }

                if (button.Authorize is not null)
                {
                    WriteAuthorize(writer, button.Authorize);
                }

                WriteOutletPosition(writer, button);
            }
        }
    }

    void WriteToolbarItem(ScreenplayWriter writer, ToolbarItemSyntax item)
    {
        using var anchor = writer.Anchor(item);
        var head = item.Kind switch
        {
            ToolbarItemKind.Navigate => $"item {item.Name} navigate to {item.Target}",
            ToolbarItemKind.Dialog => $"item {item.Name} dialog {item.Target}",
            _ => $"item {item.Name} action {item.Target}"
        };
        writer.Line(head + WriteActionSubject(item.Subject));
        using (writer.Indent())
        {
            if (item.Label is not null)
            {
                writer.Line($"label {ScreenplaySyntaxText.LocalizableString(item.Label)}");
            }

            if (item.Icon is not null)
            {
                writer.Line($"icon {item.Icon}");
            }

            if (item.Confirm is not null)
            {
                writer.Line($"confirm {ScreenplaySyntaxText.LocalizableString(item.Confirm)}");
            }

            if (item.EnabledWhen is not null)
            {
                writer.Line($"enabled when {ScreenplaySyntaxText.Condition(item.EnabledWhen)}");
            }

            foreach (var parameter in item.Parameters)
            {
                WriteBindingLine(writer, $"parameter {parameter.Name}", parameter.Binding, parameter);
            }

            foreach (var value in item.Presentation)
            {
                writer.Line($"presentation {value.Name} {ScreenplaySyntaxText.LocalizableString(value.Value)}", value);
            }
        }
    }

    void WriteScreenSummary(ScreenplayWriter writer, ScreenSummarySyntax summary)
    {
        using var anchor = writer.Anchor(summary);
        writer.Line($"summary {summary.Target}");
        using (writer.Indent())
        {
            foreach (var field in summary.Fields)
            {
                writer.Line($"field {field.Property} label {ScreenplaySyntaxText.LocalizableString(field.Label)}", field);
            }
        }
    }
}
