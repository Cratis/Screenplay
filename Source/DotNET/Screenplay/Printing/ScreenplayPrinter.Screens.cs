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
        writer.Line($"screen {screen.Name}");
        using (writer.Indent())
        {
            if (screen.File is not null)
            {
                writer.Line($"file {screen.File.Path}");
            }

            foreach (var directive in screen.Directives)
            {
                WriteScreenDirective(writer, directive);
            }
        }
    }

    void WriteScreenDirective(ScreenplayWriter writer, ScreenDirectiveSyntax directive)
    {
        switch (directive)
        {
            case ScreenDataSyntax data:
                writer.Line(WriteScreenData(data));
                break;
            case ScreenActionSyntax action:
                WriteScreenAction(writer, action);
                break;
            case ScreenNavigateSyntax navigate:
                writer.Line(WriteScreenNavigate(navigate));
                break;
            case ScreenLayoutSyntax layout:
                WriteScreenLayout(writer, layout);
                break;
            case ScreenSectionSyntax section:
                WriteScreenSection(writer, section);
                break;
            case ScreenTitleSyntax title:
                writer.Line($"title \"{title.Text}\"");
                break;
            case ScreenTableSyntax table:
                WriteScreenTable(writer, table);
                break;
            case ScreenSummarySyntax summary:
                WriteScreenSummary(writer, summary);
                break;
            case ScreenCodeSyntax code:
                WriteCodeBlock(writer, code.Code);
                break;
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

    void WriteScreenAction(ScreenplayWriter writer, ScreenActionSyntax action)
    {
        writer.Line($"action {action.Command}");
        if (action.Label is null && action.Navigate is null)
        {
            return;
        }

        using (writer.Indent())
        {
            if (action.Label is not null)
            {
                writer.Line($"label \"{action.Label}\"");
            }

            if (action.Navigate is not null)
            {
                writer.Line(WriteScreenNavigate(action.Navigate));
            }
        }
    }

    void WriteScreenLayout(ScreenplayWriter writer, ScreenLayoutSyntax layout)
    {
        writer.Line($"layout {layout.Name}");
        using (writer.Indent())
        {
            foreach (var slot in layout.Slots)
            {
                writer.Line(slot.Name);
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
        writer.Line($"table {table.Target}");
        using (writer.Indent())
        {
            foreach (var column in table.Columns)
            {
                writer.Line(column.Label is null
                    ? $"column {column.Property}"
                    : $"column {column.Property} label \"{column.Label}\"");
            }

            if (table.RowClick is not null)
            {
                writer.Line($"on row-click {WriteScreenNavigate(table.RowClick)}");
            }
        }
    }

    void WriteScreenSummary(ScreenplayWriter writer, ScreenSummarySyntax summary)
    {
        writer.Line($"summary {summary.Target}");
        using (writer.Indent())
        {
            foreach (var field in summary.Fields)
            {
                writer.Line($"field {field.Property} label \"{field.Label}\"");
            }
        }
    }
}
