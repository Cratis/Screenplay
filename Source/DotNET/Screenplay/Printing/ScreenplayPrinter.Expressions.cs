// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of inline fenced code blocks shared across policies, handlers, reactors, validation and screens.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteCodeBlock(ScreenplayWriter writer, CodeBlockSyntax code)
    {
        writer.Line(code.Language);
        using (writer.Indent())
        {
            WriteFencedCode(writer, code);
        }
    }

    void WriteFencedCode(ScreenplayWriter writer, CodeBlockSyntax code)
    {
        writer.Line("```");
        foreach (var line in code.Code.Split('\n'))
        {
            writer.Line(line);
        }

        writer.Line("```");
    }
}
