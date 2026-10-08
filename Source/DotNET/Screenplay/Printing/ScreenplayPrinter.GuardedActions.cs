// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of guarded screen actions.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteScreenGuardedAction(ScreenplayWriter writer, ScreenGuardedActionSyntax action)
    {
        writer.Line($"action {ScreenplaySyntaxText.LocalizableString(action.Label)}");
        using (writer.Indent())
        {
            foreach (var alternative in action.Alternatives)
            {
                writer.Line($"when {ScreenplaySyntaxText.Condition(alternative.Condition)} execute {alternative.Command}", alternative);
                WriteGuardedActionArguments(writer, alternative.Arguments);
            }

            if (action.Otherwise is { } otherwise)
            {
                writer.Line(otherwise.Outcome == ScreenActionOtherwiseOutcome.Hidden ? "otherwise hidden" : $"otherwise execute {otherwise.Command}", otherwise);
                WriteGuardedActionArguments(writer, otherwise.Arguments);
            }

            if (action.Navigate is not null) writer.Line(WriteScreenNavigate(action.Navigate), action.Navigate);
        }
    }

    void WriteGuardedActionArguments(ScreenplayWriter writer, IEnumerable<InteractionArgumentSyntax> arguments)
    {
        using (writer.Indent())
        {
            foreach (var argument in arguments) writer.Line($"with {argument.Name} from {argument.Binding}", argument);
        }
    }
}
