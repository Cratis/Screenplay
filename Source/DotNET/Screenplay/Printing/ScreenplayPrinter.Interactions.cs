// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

public partial class ScreenplayPrinter
{
    /// <summary>
    /// Writes a named behavior declaration.
    /// </summary>
    /// <param name="writer">The <see cref="ScreenplayWriter"/> to write to.</param>
    /// <param name="behavior">The <see cref="BehaviorSyntax"/> to write.</param>
    void WriteBehavior(ScreenplayWriter writer, BehaviorSyntax behavior)
    {
        using var anchor = writer.Anchor(behavior);
        writer.Line($"behavior {behavior.Name}");

        using (writer.Indent())
        {
            WriteDescription(writer, behavior.Description);
            WriteFile(writer, behavior.File);

            foreach (var parameter in behavior.Parameters)
            {
                writer.Line(
                    parameter.Type is null
                        ? $"parameter {parameter.Name}"
                        : $"parameter {parameter.Name} {ScreenplaySyntaxText.TypeRef(parameter.Type)}",
                    parameter);
            }

            if (behavior.Order is { } order)
            {
                writer.Line($"order {order}");
            }

            WriteInteractionBindings(writer, behavior.Bindings);
        }
    }

    /// <summary>
    /// Writes an attached behavior at the position it was attached.
    /// </summary>
    /// <param name="writer">The <see cref="ScreenplayWriter"/> to write to.</param>
    /// <param name="behavior">The <see cref="BehaviorSyntax"/> to write.</param>
    /// <remarks>
    /// An anonymous behavior round-trips as the inline <c>on</c> block it was written as. It is never hoisted
    /// into a synthesized top level declaration - a document that reorganizes itself on format is not a
    /// round trip.
    /// </remarks>
    void WriteAttachedBehavior(ScreenplayWriter writer, BehaviorSyntax behavior)
    {
        if (behavior.Name?.Length > 0)
        {
            WriteBehavior(writer, behavior);
            return;
        }

        using var anchor = writer.Anchor(behavior);
        WriteInteractionBindings(writer, behavior.Bindings);
    }

    /// <summary>
    /// Writes a <c>uses</c> attachment and its arguments.
    /// </summary>
    /// <param name="writer">The <see cref="ScreenplayWriter"/> to write to.</param>
    /// <param name="uses">The <see cref="UsesBehaviorSyntax"/> to write.</param>
    void WriteUsesBehavior(ScreenplayWriter writer, UsesBehaviorSyntax uses)
    {
        using var anchor = writer.Anchor(uses);
        writer.Line($"uses {uses.Behavior}");

        if (!uses.Arguments.Any())
        {
            return;
        }

        using (writer.Indent())
        {
            foreach (var argument in uses.Arguments)
            {
                writer.Line($"{argument.Name} {argument.Value}", argument);
            }
        }
    }

    /// <summary>
    /// Writes every behavior attached to a structure - the inline ones, then the named attachments.
    /// </summary>
    /// <param name="writer">The <see cref="ScreenplayWriter"/> to write to.</param>
    /// <param name="behaviors">The inline behaviors.</param>
    /// <param name="usedBehaviors">The named attachments.</param>
    void WriteAttachments(ScreenplayWriter writer, IEnumerable<BehaviorSyntax> behaviors, IEnumerable<UsesBehaviorSyntax> usedBehaviors)
    {
        foreach (var behavior in behaviors)
        {
            WriteAttachedBehavior(writer, behavior);
        }

        foreach (var uses in usedBehaviors)
        {
            WriteUsesBehavior(writer, uses);
        }
    }

    void WriteInteractionBindings(ScreenplayWriter writer, IEnumerable<InteractionBindingSyntax> bindings)
    {
        foreach (var binding in bindings)
        {
            WriteInteractionBinding(writer, binding);
        }
    }

    void WriteInteractionBinding(ScreenplayWriter writer, InteractionBindingSyntax binding)
    {
        using var anchor = writer.Anchor(binding);
        writer.Line($"on {ScreenplaySyntaxText.InteractionTrigger(binding.Trigger)}");

        using (writer.Indent())
        {
            if (binding.Condition is not null)
            {
                writer.Line($"where {binding.Condition}");
            }

            foreach (var action in binding.Actions)
            {
                WriteInteractionAction(writer, action);
            }
        }
    }

    void WriteInteractionAction(ScreenplayWriter writer, InteractionActionSyntax action)
    {
        using var anchor = writer.Anchor(action);
        writer.Line(ScreenplaySyntaxText.InteractionAction(action));

        var hasBody = action.Arguments.Any() || action.OnSuccess.Any() || action.OnFailure.Any() || action.OnResult.Any();
        if (!hasBody)
        {
            return;
        }

        using (writer.Indent())
        {
            foreach (var argument in action.Arguments)
            {
                writer.Line($"with {argument.Name} from {argument.Binding}", argument);
            }

            WriteContinuation(writer, "success", action.OnSuccess);
            WriteContinuation(writer, "failure", action.OnFailure);
            WriteContinuation(writer, "result", action.OnResult);
        }
    }

    void WriteContinuation(ScreenplayWriter writer, string keyword, IEnumerable<InteractionActionSyntax> actions)
    {
        if (!actions.Any())
        {
            return;
        }

        writer.Line($"on {keyword}");

        using (writer.Indent())
        {
            foreach (var action in actions)
            {
                WriteInteractionAction(writer, action);
            }
        }
    }
}
