// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

public partial class ScreenplayPrinter
{
    void WritePossibleEvents(ScreenplayWriter writer, PossibleEventsSyntax? possible)
    {
        if (possible is null) return;
        writer.Line("may produce", possible);
        using (writer.Indent())
        {
            foreach (var entry in possible.Events) WritePossibleEvent(writer, entry);
        }
    }

    void WritePossibleEvent(ScreenplayWriter writer, PossibleEventSyntax entry)
    {
        var target = entry.Property is null ? string.Empty : $" for {(entry.OneOf ? "one of " : string.Empty)}{entry.Property}";
        writer.Line($"{entry.Event}{target}", entry);
        using (writer.Indent())
        {
            if (entry.Stream is not null) WriteCommandStream(writer, entry.Stream);
            WriteTags(writer, entry.Tags);
        }
    }
}
