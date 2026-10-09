// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using Cratis.Screenplay.Processing;

namespace Cratis.Screenplay.Tool;

static class ProcessingReportText
{
    // These reports are standalone text, never HTML-embedded. Markdown escapes HTML and CSV quotes cells.
    // Keep Norwegian names and legal references readable without relaxing either output boundary.
    static readonly JsonSerializerOptions _cells = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    static readonly JsonSerializerOptions _json = new(_cells) { WriteIndented = true };
    static readonly string[] _columns = ["Purpose", "Description", "Basis", "Basis reference", "Interest", "Condition", "Condition reference", "Authorization", "Subjects", "Recipients", "Transfers", "Retention", "Erasure exception", "Personal data categories", "Special categories", "Criminal data", "Declared security measures", "DPIA prompt", "Findings", "Covered slices", "Controller name", "Controller contact", "Notice"];

    internal static void Write(ProcessingRecord report, string format, TextWriter output)
    {
        if (format == "json")
        {
            output.WriteLine(JsonSerializer.Serialize(report, _json));
            return;
        }

        if (format == "csv")
        {
            output.WriteLine(string.Join(',', _columns.Select(column => Csv(column == "Notice" ? report.Notice : column))));
            foreach (var row in report.Rows) output.WriteLine(string.Join(',', Cells(report, row).Select(Csv)));
            return;
        }

        output.WriteLine(report.Notice);
        output.WriteLine();
        output.WriteLine($"Controller: {Markdown(report.ControllerName ?? "Not supplied")} — {Markdown(report.ControllerContact ?? "Contact not supplied")}");
        output.WriteLine($"Declared purposes: {report.Rows.Count}");
        output.WriteLine(report.Coverage);
        output.WriteLine();
        output.WriteLine("| " + string.Join(" | ", _columns.Select(Markdown)) + " |");
        output.WriteLine("| " + string.Join(" | ", _columns.Select(_ => "---")) + " |");
        foreach (var row in report.Rows) output.WriteLine("| " + string.Join(" | ", Cells(report, row).Select(Markdown)) + " |");
    }

    static string[] Cells(ProcessingRecord report, ProcessingRecordRow row) =>
    [
        row.Purpose, row.Description ?? string.Empty, row.Basis ?? string.Empty, row.BasisReference ?? string.Empty, row.Interest ?? string.Empty,
        row.Condition ?? string.Empty, row.ConditionReference ?? string.Empty, row.Authorization ?? string.Empty,
        JsonSerializer.Serialize(row.Subjects, _cells), JsonSerializer.Serialize(row.Recipients, _cells), JsonSerializer.Serialize(row.Transfers, _cells), row.Retention ?? string.Empty, row.ErasureException ?? string.Empty,
        JsonSerializer.Serialize(row.Categories, _cells), JsonSerializer.Serialize(row.SpecialCategories, _cells), row.CriminalData ? "true" : "false", JsonSerializer.Serialize(row.SecurityMeasures, _cells), row.DpiaPrompt ?? string.Empty,
        JsonSerializer.Serialize(row.Findings, _cells), JsonSerializer.Serialize(row.CoveredSlices, _cells), report.ControllerName ?? string.Empty, report.ControllerContact ?? string.Empty, report.Notice
    ];

    static string Csv(string value) => '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

    static string Markdown(string value) => value.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\\", "\\\\", StringComparison.Ordinal).Replace("|", "\\|", StringComparison.Ordinal).Replace("`", "\\`", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal).Replace("]", "\\]", StringComparison.Ordinal).Replace("*", "\\*", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("\r\n", "<br>", StringComparison.Ordinal).Replace("\r", "<br>", StringComparison.Ordinal).Replace("\n", "<br>", StringComparison.Ordinal);
}
