// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of imperative validation implementations.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteValidationRequirement(ScreenplayWriter writer, RequirementSyntax requirement)
    {
        writer.Line($"require {ScreenplaySyntaxText.Condition(requirement.Condition)}", requirement);
        if (requirement.Message is null && requirement.Severity == ValidationSeverity.Error) return;
        using (writer.Indent())
        {
            if (requirement.Severity != ValidationSeverity.Error)
            {
                writer.DirectiveLine(ScreenplaySyntaxText.Severity(requirement.Severity).TrimStart(), requirement, "severity");
            }

            if (requirement.Message is not null)
            {
                writer.DirectiveLine($"message {ScreenplaySyntaxText.LocalizableString(requirement.Message)}", requirement, "message");
            }
        }
    }

    void WriteValidationImplementation(ScreenplayWriter writer, ValidateSyntax validation)
    {
        if (validation is FileValidateSyntax file) WriteFile(writer, file.File);
        if (validation is CodeValidateSyntax code) WriteFencedCode(writer, code.Code);
    }
}
