// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Diagnostics;

/// <summary>
/// Represents an implementation of <see cref="IDiagnosticFormatter"/> that renders diagnostics
/// with their offending source line and a caret pointing at the location.
/// </summary>
public class DiagnosticFormatter : IDiagnosticFormatter
{
    const string Reset = "\e[0m";
    const string Bold = "\e[1m";
    const string Red = "\e[31m";
    const string Yellow = "\e[33m";
    const string Cyan = "\e[36m";
    const string Gray = "\e[90m";

    /// <inheritdoc/>
    public string Format(string file, Diagnostic diagnostic, string source, bool useColors)
    {
        var severity = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            _ => "info"
        };

        var severityColor = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => Red,
            DiagnosticSeverity.Warning => Yellow,
            _ => Cyan
        };

        var builder = new StringBuilder();
        if (useColors)
        {
            builder.Append($"{Bold}{file}({diagnostic.Location.Line},{diagnostic.Location.Column}){Reset}: {severityColor}{severity}{Reset}: {diagnostic.Message}");
        }
        else
        {
            builder.Append($"{file}({diagnostic.Location.Line},{diagnostic.Location.Column}): {severity}: {diagnostic.Message}");
        }

        var lines = source.Split('\n');
        if (diagnostic.Location.Line >= 1 && diagnostic.Location.Line <= lines.Length)
        {
            var line = lines[diagnostic.Location.Line - 1].TrimEnd('\r');
            var lineNumber = diagnostic.Location.Line.ToString();
            var caretOffset = Math.Max(diagnostic.Location.Column - 1, 0);
            var caret = new string(' ', caretOffset) + "^";

            builder.Append('\n');
            if (useColors)
            {
                builder.Append($"{Gray}{lineNumber,5} |{Reset} {line}\n")
                    .Append($"{Gray}      |{Reset} {severityColor}{caret}{Reset}");
            }
            else
            {
                builder.Append($"{lineNumber,5} | {line}\n")
                    .Append($"      | {caret}");
            }
        }

        return builder.ToString();
    }
}
