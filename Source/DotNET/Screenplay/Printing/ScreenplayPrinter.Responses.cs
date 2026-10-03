// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of syntax-only response contracts and expectations.
/// </summary>
public partial class ScreenplayPrinter
{
    static void WriteCommandResponse(ScreenplayWriter writer, CommandSyntax command)
    {
        if (command.Response is null) return;
        using var anchor = writer.Anchor(command.Response);
        switch (command.Response)
        {
            case ScalarCommandResponseSyntax scalar:
                // Unknown sources need an escape to remain responses when an invalid draft is printed.
                // Types in other declarations never affect this per-command decision.
                var operand = command.Properties.Any(property => property.Name == scalar.Source.Property) ? scalar.Source.Property : $"@{scalar.Source.Property}";
                writer.Line($"returns {operand}", scalar);
                break;
            case RecordCommandResponseSyntax record:
                writer.Line("returns", record);
                using (writer.Indent())
                {
                    foreach (var field in record.Fields)
                    {
                        var type = field.Type is null ? string.Empty : $" {ScreenplaySyntaxText.TypeRef(field.Type)}";
                        writer.Line($"{field.Name}{type} = {field.Source.Property}", field);
                    }
                }

                break;
        }
    }

    static void WriteSpecificationReturn(ScreenplayWriter writer, SpecificationReturnSyntax? expectation)
    {
        if (expectation is null) return;
        using var anchor = writer.Anchor(expectation);
        switch (expectation)
        {
            case ScalarSpecificationReturnSyntax scalar:
                writer.Line($"then returns {ScreenplaySyntaxText.ResponseValue(scalar.Value)}", scalar);
                break;
            case RecordSpecificationReturnSyntax record:
                writer.Line("then returns", record);
                using (writer.Indent())
                {
                    foreach (var field in record.Fields)
                    {
                        writer.Line($"{field.Property} = {ScreenplaySyntaxText.ResponseValue(field.Source)}", field);
                    }
                }

                break;
        }
    }
}
