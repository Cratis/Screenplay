// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of plain command actions and their advisory presentation.
/// </summary>
public partial class ScreenplayPrinter
{
    static bool HasScreenActionBody(ScreenActionSyntax action)
    {
        if (action.Label is not null) return true;
        if (action.Navigate is not null) return true;
        return action.Inputs.Any() || action.Presentation.Any();
    }

    static void WriteActionPresentation(ScreenplayWriter writer, IEnumerable<PresentationValueSyntax> presentation)
    {
        foreach (var value in presentation)
        {
            writer.Line($"presentation {value.Name} {ScreenplaySyntaxText.LocalizableString(value.Value)}", value);
        }
    }

    void WriteScreenAction(ScreenplayWriter writer, ScreenActionSyntax action)
    {
        using var anchor = writer.Anchor(action);
        writer.Line($"action {action.Command}{WriteActionSubject(action.Subject)}");
        if (!HasScreenActionBody(action)) return;

        using (writer.Indent())
        {
            WriteScreenActionBody(writer, action);
        }
    }

    void WriteScreenActionBody(ScreenplayWriter writer, ScreenActionSyntax action)
    {
        if (action.Label is not null)
        {
            writer.DirectiveLine($"label {ScreenplaySyntaxText.LocalizableString(action.Label)}", action, "label");
        }

        foreach (var input in action.Inputs) writer.Line($"with {input.Name} from {input.Binding}", input);
        WriteActionPresentation(writer, action.Presentation);
        WriteActionNavigation(writer, action.Navigate);
    }

    void WriteActionNavigation(ScreenplayWriter writer, ScreenNavigateSyntax? navigate)
    {
        if (navigate is null) return;
        writer.Line(WriteScreenNavigate(navigate), navigate);
        using (writer.Indent())
        {
            WriteScreenNavigateBody(writer, navigate);
        }
    }
}
