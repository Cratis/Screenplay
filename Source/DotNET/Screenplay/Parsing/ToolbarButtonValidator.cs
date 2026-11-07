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
        if (collector.Toolbars.Count == 0)
        {
            return;
        }

        var slices = AllSlices(application);
        foreach (var toolbar in collector.Toolbars)
        {
            ValidateToolbar(toolbar, slices, context);
        }
    }

    /// <summary>
    /// Lists every slice of the application with its path - <c>Module.Feature[.SubFeature].Slice</c>.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to read.</param>
    /// <returns>The slices and their paths.</returns>
    internal static List<(string[] Path, SliceSyntax Slice)> AllSlices(ApplicationSyntax application) =>
        [.. application.Modules.SelectMany(module => SlicesIn(module.Features, [module.Name]))];

    /// <summary>
    /// Checks the slice a button invokes: one the document declares, unambiguous, and a State Change slice.
    /// </summary>
    /// <param name="button">The <see cref="ToolbarButtonSyntax"/> to check.</param>
    /// <param name="owner">Where the button is, as it reads in a diagnostic.</param>
    /// <param name="slices">Every slice of the application with its path.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report to.</param>
    internal static void ValidateButtonSlice(ToolbarButtonSyntax button, string owner, List<(string[] Path, SliceSyntax Slice)> slices, ParserContext context)
    {
        if (button.Slice is null)
        {
            // A built-in needs no slice, and a template's default is a placeholder a lower level gives its hook.
            if (button.Runs is not null || button.IsDefault)
            {
                return;
            }

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

    sealed class ToolbarCollector : ScreenplaySyntaxWalker
    {
        public List<ScreenToolbarSyntax> Toolbars { get; } = [];

        public override void VisitScreenToolbar(ScreenToolbarSyntax syntax)
        {
            Toolbars.Add(syntax);
            base.VisitScreenToolbar(syntax);
        }
    }
}
