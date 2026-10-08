// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of the <c>screen</c> construct - intent level directives, structural layouts and inline code.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteScreen(ScreenplayWriter writer, ScreenSyntax screen)
    {
        using var anchor = writer.Anchor(screen);
        writer.Line($"screen {screen.Name}");
        using (writer.Indent())
        {
            if (screen.File is not null)
            {
                writer.Line($"file {screen.File.Path}", screen.File);
            }

            foreach (var directive in screen.Directives)
            {
                WriteScreenDirective(writer, directive);
            }
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

        foreach (var parameter in navigate.Parameters)
        {
            writer.Line($"parameter {parameter.Name} {WriteUiBinding(parameter.Binding)}", parameter);
        }
    }

    string WriteUiBinding(UiBindingSyntax binding)
    {
        var head = binding.BindingKind switch
        {
            UiBindingKind.DataContext => $"from data {binding.Path}",
            UiBindingKind.QueryResult => string.IsNullOrWhiteSpace(binding.Path) ? $"from query {binding.Query}" : $"from query {binding.Query}.{binding.Path}",
            UiBindingKind.ComponentProperty => $"from component {binding.ComponentId}.{binding.ComponentPropertyPath ?? binding.Path}",
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
        writer.Line($"action {action.Command}");
        if (action.Label is null && action.Navigate is null)
        {
            return;
        }

        using (writer.Indent())
        {
            if (action.Label is not null)
            {
                writer.DirectiveLine($"label {ScreenplaySyntaxText.LocalizableString(action.Label)}", action, "label");
            }

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
        writer.Line($"template {template.Name}");
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
            if (component.Context is not null)
            {
                writer.Line($"context {WriteUiBinding(component.Context)}");
            }

            foreach (var property in component.Properties)
            {
                writer.Line(
                    property.Binding is not null
                        ? $"property {property.Property} {WriteUiBinding(property.Binding)}"
                        : $"property {property.Property} = {ScreenplaySyntaxText.LocalizableString(property.Value ?? string.Empty)}",
                    property);
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
                writer.Line($"exposes {exposed.Name} {WriteUiBinding(exposed.Binding)}", exposed);
            }

            foreach (var outlet in component.Outlets)
            {
                writer.Line($"outlet {outlet.Name}", outlet);
                using (writer.Indent())
                {
                    foreach (var directive in outlet.Directives)
                    {
                        WriteScreenDirective(writer, directive);
                    }
                }
            }

            WriteAttachments(writer, component.Behaviors, component.UsedBehaviors);
        }
    }

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
        writer.Line(head);
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

            foreach (var parameter in item.Parameters)
            {
                writer.Line($"parameter {parameter.Name} {WriteUiBinding(parameter.Binding)}", parameter);
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
