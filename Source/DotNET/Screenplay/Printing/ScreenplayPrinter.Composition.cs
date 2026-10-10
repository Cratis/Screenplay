// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Prints the screen composition constructs: exposures, instance contributions, template content and picker
/// metadata, and screen contributions.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteExposure(ScreenplayWriter writer, ExposureSyntax exposure)
    {
        using var anchor = writer.Anchor(exposure);
        writer.Line($"exposure for {exposure.Owner}");
        using (writer.Indent())
        {
            foreach (var property in exposure.Properties)
            {
                var line = $"property {WriteComponentReference(property.Component)}.{property.Path}";
                if (property.Label is not null) line += $" label {StringLiteral.Quote(property.Label)}";
                if (property.IsCollection) line += $" operations {List(property.Operations)}";
                if (property.RestrictsFields) line += $" fields {List(property.EditableFields)}";
                if (property.ReExposes is not null) line += $" reexposes {property.ReExposes}";
                writer.Line(line, property);
            }
        }
    }

    void WriteInstanceContributions(ScreenplayWriter writer, InstanceContributionsSyntax instance)
    {
        using var anchor = writer.Anchor(instance);
        writer.Line($"instance {instance.Instance}");
        using (writer.Indent())
        {
            foreach (var contribution in instance.Contributions)
            {
                var target = $"{WriteComponentReference(contribution.Component)}.{contribution.Path}";
                if (contribution.Value is not null)
                {
                    writer.Line($"set {target} = {writer.Expression(contribution.Value)}", contribution);
                    continue;
                }

                writer.Line($"items {target}", contribution);
                using (writer.Indent())
                {
                    foreach (var item in contribution.Items)
                    {
                        writer.Line($"item {WriteItemId(item.Id)}", item);
                        using (writer.Indent())
                        {
                            foreach (var value in item.Values)
                            {
                                writer.Line($"{value.Field} = {writer.Expression(value.Value)}", value);
                            }
                        }
                    }
                }
            }
        }
    }

    void WriteTemplateScopes(ScreenplayWriter writer, bool restrictsScopes, IEnumerable<string> scopes)
    {
        if (restrictsScopes)
        {
            writer.Line($"scopes {List(scopes)}");
        }
    }

    void WriteTemplatePicker(ScreenplayWriter writer, string? displayName, string? description, bool restrictsScopes, IEnumerable<string> scopes)
    {
        if (displayName is not null)
        {
            writer.Line($"display {StringLiteral.Quote(displayName)}");
        }

        if (description is not null)
        {
            writer.Line($"description {StringLiteral.Quote(description)}");
        }

        WriteTemplateScopes(writer, restrictsScopes, scopes);
    }

    void WriteTemplateContent(ScreenplayWriter writer, IEnumerable<TemplateSlotContentSyntax> content)
    {
        foreach (var slotContent in content)
        {
            writer.Line($"content {slotContent.Slot}", slotContent);
            using (writer.Indent())
            {
                foreach (var directive in slotContent.Directives)
                {
                    WriteScreenDirective(writer, directive);
                }
            }
        }
    }

    void WriteScreenContributions(ScreenplayWriter writer, IEnumerable<ScreenContributionSyntax> contributions)
    {
        foreach (var contribution in contributions)
        {
            writer.Line(
                contribution.Order is null
                    ? $"contribute to {contribution.ContributionPoint}"
                    : $"contribute to {contribution.ContributionPoint} order {contribution.Order}",
                contribution);
            using (writer.Indent())
            {
                foreach (var directive in contribution.Directives)
                {
                    WriteScreenDirective(writer, directive);
                }
            }
        }
    }

    string WriteArrangementContainerLine(string keyword, ArrangementContainerSyntax container)
    {
        var line = keyword;
        if (container.Gap is not null) line += $" gap {container.Gap}";
        if (container.Columns is not null) line += $" columns {container.Columns}";
        if (container.Rows is not null) line += $" rows {container.Rows}";
        if (container.Grow is not null) line += container.Grow == 1d ? " grow" : $" grow {WriteNumber(container.Grow.Value)}";
        if (container.Span is not null) line += $" span {container.Span}";
        return line;
    }

    string List(IEnumerable<string> values) => values.Any() ? string.Join(", ", values) : "none";

    string WriteNumber(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    string WriteComponentReference(string component) => WriteStableComponentId(component);

    string WriteItemId(string id) =>
        id.Length > 0 && (char.IsLetter(id[0]) || id[0] == '_') && id.All(character => char.IsLetterOrDigit(character) || character is '_' or '-')
            ? id
            : StringLiteral.Quote(id);

    void WriteNavigationItem(ScreenplayWriter writer, ContributionSyntax contribution)
    {
        if (contribution.Id is not null) writer.Line($"id {WriteItemValue(contribution.Id)}", contribution);
        if (contribution.Icon is not null) writer.Line($"icon {WriteItemValue(contribution.Icon)}", contribution);
        if (contribution.Presentation is not null) writer.Line($"presentation {WriteItemValue(contribution.Presentation)}", contribution);
        if (contribution.Group is not null) writer.Line($"group {WriteItemValue(contribution.Group)}", contribution);
        if (contribution.Destination is { } destination)
        {
            writer.Line(
                destination.Kind switch
                {
                    ContributionDestinationKind.Outlet => $"destination outlet {destination.Target}",
                    ContributionDestinationKind.Dialog => $"destination dialog {destination.Target}",
                    _ => $"destination external {StringLiteral.Quote(destination.Target)}"
                },
                destination);
        }
    }

    string WriteItemValue(string value) =>
        value.Length > 0 && (char.IsLetter(value[0]) || value[0] == '_') && value.All(character => char.IsLetterOrDigit(character) || character is '_' or '.' or '-')
            ? value
            : StringLiteral.Quote(value);
}
