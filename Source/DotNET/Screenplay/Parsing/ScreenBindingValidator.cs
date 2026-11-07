// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks what a slice screen's elements are bound to: <c>show &lt;Screen&gt; by &lt;param&gt; from &lt;element&gt;.selection</c>
/// lines, and components bound to nothing.
/// </summary>
internal static class ScreenBindingValidator
{
    /// <summary>
    /// Checks every slice screen of an application.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to check.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report to.</param>
    public static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var slices = ToolbarButtonValidator.AllSlices(application).ConvertAll(entry => entry.Slice);
        var screens = slices.SelectMany(slice => slice.Screens).ToList();
        foreach (var slice in slices)
        {
            foreach (var screen in slice.Screens)
            {
                var elements = All(screen.Directives).ToList();
                foreach (var show in elements.OfType<ScreenShowSyntax>())
                {
                    CheckShow(show, screen, elements, screens, context);
                }

                if (!DisplaysData(slice, elements))
                {
                    continue;
                }

                // A component a slot shows the selection of is something the user picks from, not text.
                var selectionSources = elements.OfType<ScreenShowSyntax>().Select(show => show.Element).ToHashSet(StringComparer.Ordinal);
                foreach (var component in elements.OfType<ScreenComponentSyntax>().Where(component => IsUnbound(component) && !selectionSources.Contains(component.Name)))
                {
                    context.Warning(
                        DiagnosticCodes.UnboundScreenText,
                        $"Component '{component.Name}' on screen '{screen.Name}' shows data but is bound to nothing and given no value, so it can only show sample text - bind it with 'context' or 'property <path> from <binding>', or give it static text with 'property <path> = \"...\"'",
                        component.Location);
                }
            }
        }
    }

    // A screen displays bound data when its slice reads state or the screen binds data. A screen of a State Change slice
    // with no data is a form: a component on it without a binding is an input or decoration, not text left unbound.
    static bool DisplaysData(SliceSyntax slice, List<ScreenDirectiveSyntax> elements) =>
        slice.Type == SliceType.StateView || elements.Exists(element => element is ScreenDataSyntax or ScreenTableSyntax or ScreenSummarySyntax);

    static bool IsUnbound(ScreenComponentSyntax component) =>
        component.Context is null && !component.Properties.Any() && !component.Exposes.Any() && !component.Slots.Any() && !component.Behaviors.Any() && !component.UsedBehaviors.Any();

    static void CheckShow(ScreenShowSyntax show, ScreenSyntax screen, List<ScreenDirectiveSyntax> elements, List<ScreenSyntax> screens, ParserContext context)
    {
        var inSlot = screen.Directives.OfType<ScreenTemplateReferenceSyntax>().Any(template => template.Slots.Any(slot => slot.Directives.Contains(show)));
        if (!inSlot)
        {
            context.Error(DiagnosticCodes.InvalidScreenShow, $"'show {show.Screen}' on screen '{screen.Name}' stands outside a template slot - a slot shows another screen", show.Location);
            return;
        }

        var name = show.Screen.Split('.')[^1];
        var target = screens.Find(candidate => candidate.Name == name);
        if (target is null)
        {
            context.Error(DiagnosticCodes.InvalidScreenShow, $"Screen '{screen.Name}' shows screen '{show.Screen}', which the document does not declare", show.Location);
            return;
        }

        var hasElement = elements.Exists(element => element switch
        {
            ScreenTableSyntax table => table.Target == show.Element,
            ScreenComponentSyntax component => component.Name == show.Element,
            _ => false
        });
        if (!hasElement)
        {
            context.Error(DiagnosticCodes.InvalidScreenShow, $"Screen '{screen.Name}' shows '{show.Screen}' from '{show.Element}.selection', but has no table or component '{show.Element}'", show.Location);
            return;
        }

        var keys = All(target.Directives).OfType<ScreenDataSyntax>().Select(data => data.By).OfType<string>().ToList();
        if (keys.Count > 0 && !keys.Contains(show.By, StringComparer.Ordinal))
        {
            context.Error(DiagnosticCodes.InvalidScreenShow, $"Screen '{screen.Name}' shows '{show.Screen}' by '{show.By}', but '{show.Screen}' is keyed by {string.Join(", ", keys.Select(key => $"'{key}'"))}", show.Location);
        }
    }

    static IEnumerable<ScreenDirectiveSyntax> All(IEnumerable<ScreenDirectiveSyntax> directives) =>
        directives.SelectMany(directive => All(Children(directive)).Prepend(directive));

    static IEnumerable<ScreenDirectiveSyntax> Children(ScreenDirectiveSyntax directive) => directive switch
    {
        ScreenTemplateReferenceSyntax template => template.Slots.SelectMany(slot => slot.Directives),
        ScreenSlotSyntax slot => slot.Directives,
        ScreenSectionSyntax section => section.Directives,
        ScreenComponentSyntax component => component.Slots.SelectMany(slot => slot.Directives),
        _ => []
    };
}
