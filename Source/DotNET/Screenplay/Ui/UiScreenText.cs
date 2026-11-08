// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Ui;

/// <summary>
/// Spells screen syntax the way the UI contract writes it.
/// </summary>
static class UiScreenText
{
    internal static string? FirstTitle(IEnumerable<ScreenDirectiveSyntax> directives)
    {
        foreach (var directive in directives)
        {
            var title = directive switch
            {
                ScreenTitleSyntax found => found.Text,
                ScreenTemplateReferenceSyntax template => template.Slots.Select(slot => FirstTitle(slot.Directives)).FirstOrDefault(text => text is not null),
                ScreenSlotSyntax slot => FirstTitle(slot.Directives),
                ScreenSectionSyntax section => FirstTitle(section.Directives),
                _ => null
            };
            if (title is not null) return title;
        }

        return null;
    }

    internal static string Subject(ScreenActionSubjectKind? subject) => subject == ScreenActionSubjectKind.Item ? "item" : "screen";

    internal static string? Opens(ToolbarButtonSyntax button) => button.Opens switch
    {
        HookOpening.Dialog => "dialog",
        HookOpening.Drawer => "drawer",
        HookOpening.Slot => button.OpensSlot,
        _ => null
    };

    internal static UiNavigation Navigation(ScreenNavigateSyntax navigate, string trigger) =>
        new(navigate.Screen, trigger) { By = navigate.By, Opens = navigate.Slot };

    internal static string TypeName(TypeRefSyntax type) => $"{type.Name}{(type.IsCollection ? "[]" : string.Empty)}{(type.IsOptional ? "?" : string.Empty)}";

    internal static string Trigger(InteractionTriggerSyntax trigger) => trigger switch
    {
        BuiltInInteractionTriggerSyntax builtIn => Kebab(builtIn.Kind.ToString()),
        EventInteractionTriggerSyntax raised => $"event {raised.EventName}",
        ApplicationTriggerInteractionTriggerSyntax application => $"trigger {application.TriggerName}",
        IntervalInteractionTriggerSyntax interval => $"every {interval.Amount} {interval.Unit.ToString().ToLowerInvariant()}",
        _ => "unknown"
    };

    static string Kebab(string name) => string.Concat(name.Select((character, index) => char.IsUpper(character) && index > 0 ? $"-{char.ToLowerInvariant(character)}" : $"{char.ToLowerInvariant(character)}"));
}
