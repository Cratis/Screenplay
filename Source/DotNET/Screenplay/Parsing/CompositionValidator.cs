// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates the screen composition a document declares: who exposes what, who configures what was exposed,
/// where navigation opens, which scopes a template may be used at, and which packages components come from.
/// </summary>
/// <remarks>
/// Every finding is an error so a renderer never receives a composition it would have to guess about. The
/// source stays in the syntax tree, so an authoring tool can still show and repair it.
/// </remarks>
internal static class CompositionValidator
{
    /// <summary>
    /// Validates the composition of an application.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    public static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var collector = new CompositionCollector();
        collector.VisitApplication(application);

        ValidateExposures(application, collector, context);
        ValidateInstances(application, collector, context);
        ValidateOutlets(collector, context);
        ValidateTemplateScopes(application, context);
        ValidatePackages(application, collector, context);
    }

    static void ValidateExposures(ApplicationSyntax application, CompositionCollector collector, ParserContext context)
    {
        var exposed = application.Exposures
            .SelectMany(exposure => exposure.Properties.Select(property => (exposure.Owner, Property: property)))
            .ToList();

        foreach (var exposure in application.Exposures.Where(exposure => !collector.Structures.Contains(exposure.Owner)))
        {
            context.Error(
                DiagnosticCodes.UnknownExposureOwner,
                $"Unknown exposure owner '{exposure.Owner}' - an exposure belongs to a layout, screen template or dialog template",
                exposure.Location);
        }

        foreach (var (owner, property) in exposed.Where(entry => entry.Property.ReExposes is not null))
        {
            var passedOn = exposed.Exists(entry =>
                entry.Owner == property.ReExposes &&
                entry.Property.Component == property.Component &&
                entry.Property.Path == property.Path);
            if (!passedOn)
            {
                context.Error(
                    DiagnosticCodes.ReExposureBroken,
                    $"'{owner}' re-exposes '{property.Component}.{property.Path}' from '{property.ReExposes}', which does not expose it",
                    property.Location);
            }
            else if (FormsCycle(owner, property, exposed))
            {
                context.Error(
                    DiagnosticCodes.ReExposureCycle,
                    $"Re-exposing '{property.Component}.{property.Path}' from '{owner}' forms a cycle - a re-exposure must lead back to the owner that exposes it first",
                    property.Location);
            }
        }
    }

    static bool FormsCycle(string owner, ExposedPropertySyntax property, IReadOnlyList<(string Owner, ExposedPropertySyntax Property)> exposed)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal) { owner };
        var next = property.ReExposes;
        while (next is not null)
        {
            if (!visited.Add(next))
            {
                return true;
            }

            next = exposed
                .Where(entry => entry.Owner == next && entry.Property.Component == property.Component && entry.Property.Path == property.Path)
                .Select(entry => entry.Property.ReExposes)
                .FirstOrDefault();
        }

        return false;
    }

    static void ValidateInstances(ApplicationSyntax application, CompositionCollector collector, ParserContext context)
    {
        var exposed = application.Exposures.SelectMany(exposure => exposure.Properties).ToList();
        foreach (var instance in application.InstanceContributions)
        {
            if (!collector.Screens.Contains(instance.Instance) && !collector.Templates.Contains(instance.Instance))
            {
                context.Error(
                    DiagnosticCodes.UnknownContributionInstance,
                    $"Unknown instance '{instance.Instance}' - instance values belong to a screen, screen template or dialog template",
                    instance.Location);
            }

            foreach (var contribution in instance.Contributions)
            {
                var matches = exposed.Where(property => property.Component == contribution.Component && property.Path == contribution.Path).ToList();
                if (matches.Count == 0)
                {
                    context.Error(
                        DiagnosticCodes.ContributionNotExposed,
                        $"'{instance.Instance}' stores a value for '{contribution.Component}.{contribution.Path}', which no exposure exposes",
                        contribution.Location);
                    continue;
                }

                var isCollection = matches.Exists(property => property.IsCollection);
                if (isCollection && contribution.Value is not null)
                {
                    context.Error(
                        DiagnosticCodes.ContributionTypeMismatch,
                        $"'{contribution.Component}.{contribution.Path}' is exposed as a collection - contribute 'items', not 'set'",
                        contribution.Location);
                }
                else if (!isCollection && contribution.Value is null)
                {
                    context.Error(
                        DiagnosticCodes.ContributionTypeMismatch,
                        $"'{contribution.Component}.{contribution.Path}' is not exposed as a collection - contribute a value with 'set', not 'items'",
                        contribution.Location);
                }
            }
        }
    }

    static void ValidateOutlets(CompositionCollector collector, ParserContext context)
    {
        foreach (var navigate in collector.Navigations.Where(navigate => navigate.Outlet is not null && !collector.Outlets.Contains(navigate.Outlet)))
        {
            context.Error(
                DiagnosticCodes.UnknownNavigationOutlet,
                $"Unknown outlet '{navigate.Outlet}' - no layout, template or component declares 'outlet {navigate.Outlet}'",
                navigate.Location);
        }
    }

    static void ValidateTemplateScopes(ApplicationSyntax application, ParserContext context)
    {
        var scopes = application.Modules
            .SelectMany(module => module.ScreenTemplates)
            .Where(template => template.RestrictsScopes)
            .GroupBy(template => template.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Scopes.ToList(), StringComparer.Ordinal);

        foreach (var module in application.Modules)
        {
            CheckAssignments(module.Templates, "module", scopes, context);
            foreach (var (feature, depth) in Features(module.Features, 0))
            {
                CheckAssignments(feature.Templates, depth == 0 ? "feature" : "subfeature", scopes, context);
                foreach (var slice in feature.Slices)
                {
                    CheckAssignments(slice.Templates, "slice", scopes, context);
                    foreach (var reference in slice.Screens.SelectMany(screen => screen.Directives).OfType<ScreenTemplateReferenceSyntax>())
                    {
                        Check(reference.Name, "slice", reference.Location, scopes, context);
                    }
                }
            }
        }
    }

    static void CheckAssignments(IEnumerable<TemplateAssignmentSyntax> assignments, string scope, Dictionary<string, List<string>> scopes, ParserContext context)
    {
        foreach (var assignment in assignments)
        {
            Check(assignment.Name, scope, assignment.Location, scopes, context);
        }
    }

    static void Check(string template, string scope, SourceLocation location, Dictionary<string, List<string>> scopes, ParserContext context)
    {
        if (scopes.TryGetValue(template, out var allowed) && !allowed.Contains(scope, StringComparer.Ordinal))
        {
            var allows = allowed.Count == 0 ? "no scope" : string.Join(", ", allowed);
            context.Error(
                DiagnosticCodes.TemplateScopeMismatch,
                $"The screen template '{template}' cannot be used at {scope} scope - it allows {allows}",
                location);
        }
    }

    static IEnumerable<(FeatureSyntax Feature, int Depth)> Features(IEnumerable<FeatureSyntax> features, int depth)
    {
        foreach (var feature in features)
        {
            yield return (feature, depth);
            foreach (var nested in Features(feature.Features, depth + 1))
            {
                yield return nested;
            }
        }
    }

    static void ValidatePackages(ApplicationSyntax application, CompositionCollector collector, ParserContext context)
    {
        var packages = (application.UiProfiles ?? []).SelectMany(profile => profile.Packages).ToHashSet(StringComparer.Ordinal);
        if (packages.Count == 0)
        {
            return;
        }

        foreach (var component in collector.Components)
        {
            var separator = component.Component.LastIndexOf('.');
            var package = separator < 0 ? component.Component : component.Component[..separator];
            if (!packages.Contains(package))
            {
                context.Error(
                    DiagnosticCodes.IncompatibleComponentPackage,
                    $"The component '{component.Component}' comes from package '{package}', which no ui profile declares - add it to a profile's 'packages'",
                    component.Location);
            }
        }
    }

    /// <summary>
    /// Collects the composition-relevant declarations of a document in one walk.
    /// </summary>
    sealed class CompositionCollector : ScreenplaySyntaxWalker
    {
        public HashSet<string> Structures { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Templates { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Screens { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Outlets { get; } = new(StringComparer.Ordinal);

        public List<ScreenNavigateSyntax> Navigations { get; } = [];

        public List<ScreenComponentSyntax> Components { get; } = [];

        public override void VisitNode(SyntaxNode node)
        {
            switch (node)
            {
                case LayoutSyntax layout:
                    Structures.Add(layout.Name);
                    Outlets.UnionWith(layout.Outlets.Select(outlet => outlet.Name));
                    break;
                case ScreenTemplateSyntax template:
                    Structures.Add(template.Name);
                    Templates.Add(template.Name);
                    Outlets.UnionWith(template.Outlets.Select(outlet => outlet.Name));
                    break;
                case DialogTemplateSyntax template:
                    Structures.Add(template.Name);
                    Templates.Add(template.Name);
                    Outlets.UnionWith(template.Outlets.Select(outlet => outlet.Name));
                    break;
                case ScreenSyntax screen:
                    Screens.Add(screen.Name);
                    break;
                case ComponentOutletSyntax outlet:
                    Outlets.Add(outlet.Name);
                    break;
                case ScreenNavigateSyntax navigate:
                    Navigations.Add(navigate);
                    break;
                case ScreenComponentSyntax component:
                    Components.Add(component);
                    break;
            }
        }
    }
}
