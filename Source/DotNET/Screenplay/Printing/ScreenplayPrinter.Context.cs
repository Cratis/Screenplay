// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

public partial class ScreenplayPrinter
{
    void WriteApplicationContext(ScreenplayWriter writer, ApplicationContextSyntax? context)
    {
        if (context is null) return;
        using var anchor = writer.Anchor(context);
        writer.Line("context");
        using (writer.Indent())
        {
            WriteDescription(writer, context.Description, context);
            foreach (var member in context.Members)
            {
                using var memberAnchor = writer.Anchor(member);
                var name = member.Name == "description" ? "@description" : member.Name;
                writer.Line($"{name} time zone");
            }
        }

        writer.Blank();
    }
}
