// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates the buttons of every toolbar in a document: ids are unique within a toolbar, and a button's <c>slice</c> names a
/// State Change slice the document declares.
/// </summary>
/// <remarks>
/// The slice reference is a dotted path - <c>Module.Feature.Slice</c> - and any unambiguous suffix of it resolves, the same way
/// other references narrow by scope. The rules mirror Scene's <c>toolbarButtonSliceNotFound</c>,
/// <c>toolbarButtonSliceNotStateChange</c> and <c>toolbarButtonSliceEmpty</c> diagnostics.
/// </remarks>
internal static class ToolbarButtonValidator
{
    /// <summary>
    /// Validates every toolbar button of the application.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    public static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var collector = new ToolbarCollector();
        collector.VisitApplication(application);
        if (collector.Toolbars.Count == 0 && collector.RegionButtons.Count == 0)
        {
            return;
        }

        var slices = application.Modules.SelectMany(module => SlicesIn(module.Features, [module.Name])).ToList();
        foreach (var toolbar in collector.Toolbars)
        {
            ValidateToolbar(toolbar, slices, context);
        }

        foreach (var (region, buttons) in collector.RegionButtons)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var button in buttons)
            {
                if (!ids.Add(button.Id))
                {
                    context.Error(DiagnosticCodes.DuplicateToolbarButton, $"The navigation topbar already has a button with the id '{button.Id}'", button.Location);
                }

                ValidateButtonSlice(button, $"the navigation {RegionName(region)}", slices, context);
            }
        }
    }

    static string RegionName(NavigationRegionSyntax region) => region.Region == NavigationRegionKind.Sidebar ? "sidebar" : "topbar";

    static IEnumerable<(string[] Path, SliceSyntax Slice)> SlicesIn(IEnumerable<FeatureSyntax> features, IEnumerable<string> prefix)
    {
        foreach (var feature in features)
        {
            string[] path = [.. prefix, feature.Name];
            foreach (var slice in feature.Slices)
            {
                yield return ([.. path, slice.Name], slice);
            }

            foreach (var nested in SlicesIn(feature.Features, path))
            {
                yield return nested;
            }
        }
    }

    static void ValidateToolbar(ScreenToolbarSyntax toolbar, List<(string[] Path, SliceSyntax Slice)> slices, ParserContext context)
    {
        var ids = new HashSet<string>(toolbar.Items.Select(item => item.Name), StringComparer.Ordinal);
        foreach (var button in toolbar.Buttons)
        {
            if (!ids.Add(button.Id))
            {
                context.Error(DiagnosticCodes.DuplicateToolbarButton, $"Toolbar '{toolbar.Name}' already has an item or button with the id '{button.Id}'", button.Location);
            }

            ValidateButtonSlice(button, $"toolbar '{toolbar.Name}'", slices, context);
        }
    }

    static void ValidateButtonSlice(ToolbarButtonSyntax button, string owner, List<(string[] Path, SliceSyntax Slice)> slices, ParserContext context)
    {
        if (button.Slice is null)
        {
            context.Warning(DiagnosticCodes.ToolbarButtonWithoutSlice, $"Toolbar button '{button.Id}' in {owner} names no slice - add 'slice <Module>.<Feature>.<Slice>' so it invokes something", button.Location);
            return;
        }

        var wanted = button.Slice.Split('.');
        var matches = slices.Where(entry => entry.Path.Length >= wanted.Length && entry.Path.TakeLast(wanted.Length).SequenceEqual(wanted)).ToList();
        if (matches.Count == 0)
        {
            context.Error(DiagnosticCodes.ToolbarButtonSliceNotFound, $"Toolbar button '{button.Id}' invokes slice '{button.Slice}', which the document does not declare", button.Location);
        }
        else if (matches.Count > 1)
        {
            context.Error(DiagnosticCodes.ToolbarButtonSliceAmbiguous, $"Toolbar button '{button.Id}' invokes slice '{button.Slice}', which matches {matches.Count} slices ({string.Join(", ", matches.Select(entry => string.Join('.', entry.Path)))}) - qualify it with the feature or module", button.Location);
        }
        else if (matches[0].Slice.Type != SliceType.StateChange)
        {
            context.Error(DiagnosticCodes.ToolbarButtonSliceNotStateChange, $"Toolbar button '{button.Id}' invokes slice '{button.Slice}', a {matches[0].Slice.Type} slice - a button invokes State Change slices only", button.Location);
        }
    }

    sealed class ToolbarCollector : ScreenplaySyntaxWalker
    {
        public List<ScreenToolbarSyntax> Toolbars { get; } = [];

        public List<(NavigationRegionSyntax Region, IEnumerable<ToolbarButtonSyntax> Buttons)> RegionButtons { get; } = [];

        public override void VisitNavigationRegion(NavigationRegionSyntax syntax)
        {
            if (syntax.Buttons.Any())
            {
                RegionButtons.Add((syntax, syntax.Buttons));
            }

            base.VisitNavigationRegion(syntax);
        }

        public override void VisitScreenToolbar(ScreenToolbarSyntax syntax)
        {
            Toolbars.Add(syntax);
            base.VisitScreenToolbar(syntax);
        }
    }
}
