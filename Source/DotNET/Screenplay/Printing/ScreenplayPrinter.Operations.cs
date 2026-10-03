// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

public partial class ScreenplayPrinter
{
    static readonly HashSet<string> _operationInputKeywords = new(StringComparer.Ordinal) { "uses", "description", "for", "tag", "generation", "origin", "documentation", "namespace", "sequence", "correlation", "causation", "causedBy", "occurred" };

    void WriteSystem(ScreenplayWriter writer, SystemSyntax system)
    {
        using var anchor = writer.Anchor(system);
        writer.Line($"system {system.Name}");
        using (writer.Indent()) WriteDescription(writer, system.Description, system);
    }

    void WriteOperation(ScreenplayWriter writer, OperationSyntax operation)
    {
        using var anchor = writer.Anchor(operation);
        writer.Line($"operation {operation.Name}");
        using (writer.Indent()) WriteOperationBody(writer, operation);
    }

    void WriteOperationBody(ScreenplayWriter writer, OperationSyntax operation, IEnumerable<PropertyMappingSyntax>? mappings = null)
    {
        using var anchor = writer.Anchor(operation);
        WriteDescription(writer, operation.Description, operation);
        writer.DirectiveLine($"uses {operation.Uses}", operation, "uses");
        if (mappings is null)
        {
            WriteProperties(writer, operation.Inputs, _operationInputKeywords);
        }
        else
        {
            foreach (var (input, mapping) in operation.Inputs.Zip(mappings))
            {
                using var inputAnchor = writer.Anchor(input);
                writer.Line($"{ReservedWords.Escape(input.Name, _operationInputKeywords)} {ScreenplaySyntaxText.TypeRef(input.Type)} = {ScreenplaySyntaxText.Expression(mapping.Source)}", mapping);
            }
        }
        if (operation.Execute is not null) WriteOperationPhase(writer, "execute", operation.Execute);
        if (operation.Compensate is not null) WriteOperationPhase(writer, "compensate", operation.Compensate);
    }

    void WriteOperationPhase(ScreenplayWriter writer, string name, OperationPhaseSyntax phase)
    {
        OperationInvariants.Validate(phase);
        using var anchor = writer.Anchor(phase);
        writer.Line(name);
        using (writer.Indent())
        {
            WriteDescription(writer, phase.Description, phase);
            if (phase.Implementation is { } implementation)
            {
                using var implementationAnchor = writer.Anchor(implementation);
                writer.Line("implementation");
                using (writer.Indent())
                {
                    foreach (var hint in implementation.Hints)
                    {
                        ImplementationInvariants.Validate(hint);
                        writer.Line($"hint {StringLiteral.Quote(hint.Text)}", hint);
                    }
                    WriteFile(writer, phase.File);
                    if (phase.Code is not null) WriteCodeBlock(writer, phase.Code);
                }
            }
            else
            {
                WriteFile(writer, phase.File);
                if (phase.Code is not null) WriteCodeBlock(writer, phase.Code);
            }
        }
    }
}
