// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of inline fenced code blocks shared across policies, handlers, reactions, validation and screens.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteCodeBlock(ScreenplayWriter writer, CodeBlockSyntax code)
    {
        WriteFencedCode(writer, code);
    }

    void WriteFencedCode(ScreenplayWriter writer, CodeBlockSyntax code) => WriteFencedText(writer, code.Code, code.Language);

    void WriteFencedText(ScreenplayWriter writer, string text, string language = "text")
    {
        writer.Line($"```{language}");
        foreach (var line in text.Split('\n'))
        {
            writer.Line(line);
        }

        writer.Line("```");
    }
}
