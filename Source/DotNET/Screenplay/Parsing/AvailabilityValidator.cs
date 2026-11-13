// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks the <c>audience</c> and <c>availability</c> declarations and every <c>available</c> gate that names them.
/// </summary>
internal static class AvailabilityValidator
{
    /// <summary>
    /// Validates the audiences, availabilities and gates of an application.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    internal static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var audiences = Declare(application.Audiences, audience => audience.Name, audience => audience.Location, DiagnosticCodes.DuplicateAudience, "audience", context);
        var availabilities = Declare(application.Availabilities, availability => availability.Name, availability => availability.Location, DiagnosticCodes.DuplicateAvailability, "availability", context);

        foreach (var persona in application.Personas ?? [])
        {
            var named = persona.Audiences.ToList();
            for (var index = 0; index < named.Count; index++)
            {
                if (audiences.ContainsKey(named[index]))
                {
                    continue;
                }

                var location = persona.DirectiveLocations?.GetValueOrDefault(DirectiveLocationKeys.ForValue("audience", named, index)) ?? persona.Location;
                context.Warning(DiagnosticCodes.UnknownAudience, $"Unknown audience '{named[index]}' - declare it with 'audience {named[index]}'", location);
            }
        }

        foreach (var availability in application.Availabilities)
        {
            ValidateReferences(availability.Requirements, audiences, availabilities.Keys.ToHashSet(StringComparer.Ordinal), context);
        }

        foreach (var gate in Gates(application))
        {
            ValidateReferences([gate.Requirement], audiences, availabilities.Keys.ToHashSet(StringComparer.Ordinal), context);
        }

        ReportCycles(application.Availabilities, availabilities.Keys.ToHashSet(StringComparer.Ordinal), context);
    }

    /// <summary>
    /// Gets every <c>available</c> gate in an application.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to inspect.</param>
    /// <returns>The gates, in document order.</returns>
    internal static IEnumerable<AvailableSyntax> Gates(ApplicationSyntax application)
    {
        var collector = new GateCollector();
        collector.VisitApplication(application);
        return collector.Gates;
    }

    static Dictionary<string, SourceLocation> Declare<T>(
        IEnumerable<T> declarations,
        Func<T, string> name,
        Func<T, SourceLocation> location,
        string code,
        string keyword,
        ParserContext context)
    {
        var declared = new Dictionary<string, SourceLocation>(StringComparer.Ordinal);
        foreach (var declaration in declarations)
        {
            if (!declared.TryAdd(name(declaration), location(declaration)))
            {
                context.Error(code, $"Duplicate {keyword} '{name(declaration)}' - declare each {keyword} once", location(declaration));
            }
        }

        return declared;
    }

    static void ValidateReferences(IEnumerable<AvailabilityRequirementSyntax> requirements, Dictionary<string, SourceLocation> audiences, HashSet<string> availabilities, ParserContext context)
    {
        foreach (var requirement in requirements)
        {
            foreach (var audience in AvailabilityRequirementSyntax.Leaves<AudienceAvailabilityRequirementSyntax>(requirement).Where(audience => !audiences.ContainsKey(audience.Audience)))
            {
                context.Warning(DiagnosticCodes.UnknownAudience, $"Unknown audience '{audience.Audience}' - declare it with 'audience {audience.Audience}'", audience.Location);
            }

            foreach (var reference in AvailabilityRequirementSyntax.Leaves<AvailabilityReferenceSyntax>(requirement).Where(reference => !availabilities.Contains(reference.Availability)))
            {
                context.Warning(DiagnosticCodes.UnknownAvailability, $"Unknown availability '{reference.Availability}' - declare it with 'availability {reference.Availability}'", reference.Location);
            }
        }
    }

    static void ReportCycles(IEnumerable<AvailabilitySyntax> availabilities, HashSet<string> known, ParserContext context)
    {
        var edges = availabilities
            .GroupBy(availability => availability.Name, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(availability => availability.Requirements)
                    .SelectMany(AvailabilityRequirementSyntax.Leaves<AvailabilityReferenceSyntax>)
                    .Select(reference => reference.Availability)
                    .Where(known.Contains)
                    .Distinct()
                    .ToList(),
                StringComparer.Ordinal);
        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var availability in availabilities)
        {
            var path = new List<string>();
            if (!reported.Contains(availability.Name) && Reaches(availability.Name, availability.Name, edges, [], path))
            {
                reported.UnionWith(path);
                context.Error(
                    DiagnosticCodes.AvailabilityCycle,
                    $"Availability '{availability.Name}' builds on itself through {string.Join(" -> ", [.. path, availability.Name])} - break the cycle",
                    availability.Location);
            }
        }
    }

    static bool Reaches(string start, string current, Dictionary<string, List<string>> edges, HashSet<string> visited, List<string> path)
    {
        path.Add(current);
        foreach (var next in edges.GetValueOrDefault(current) ?? [])
        {
            if (next == start || (visited.Add(next) && Reaches(start, next, edges, visited, path)))
            {
                return true;
            }
        }

        path.RemoveAt(path.Count - 1);
        return false;
    }

    sealed class GateCollector : ScreenplaySyntaxWalker
    {
        public List<AvailableSyntax> Gates { get; } = [];

        public override void VisitAvailable(AvailableSyntax syntax)
        {
            Gates.Add(syntax);
            base.VisitAvailable(syntax);
        }
    }
}
