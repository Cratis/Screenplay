// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of storylines: named, ordered paths through the model.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteStoryline(ScreenplayWriter writer, StorylineSyntax storyline)
    {
        using var anchor = writer.Anchor(storyline);
        writer.Line(storyline.Title is null ? $"storyline {storyline.Name}" : $"storyline {storyline.Name} {StringLiteral.Quote(storyline.Title)}");
        using (writer.Indent())
        {
            WriteDescription(writer, storyline.Description, storyline);
            foreach (var step in storyline.Steps)
            {
                var line = $"{StorylineParser.Keyword(step.ElementKind)} {step.Target}";
                writer.Line(step.Note is null ? line : $"{line} {StringLiteral.Quote(step.Note)}", step);
            }
        }
    }
}
