// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates inline declaration uniqueness and the command's event-source intent.
/// </summary>
internal static class InlineEventValidator
{
    /// <summary>
    /// Validates declarations and destinations without changing legacy production defaults.
    /// </summary>
    /// <param name="application">The application and its imports.</param>
    /// <param name="slices">All application slices.</param>
    /// <param name="context">The diagnostic sink.</param>
    public static void Validate(ApplicationSyntax application, IReadOnlyList<SliceSyntax> slices, ParserContext context)
    {
        var declarations = slices.SelectMany(EventDeclarations.In).ToLookup(value => value.Name, StringComparer.Ordinal);
        foreach (var command in slices.SelectMany(slice => slice.Commands))
        {
            var identifier = command.Properties.SingleOrDefault(property => property.IsIdentifier)?.Name;
            var mixed = command.Produces.Any(value => value.For is not null &&
                (value.For is not PathExpressionSyntax path || path.Path != identifier));
            foreach (var production in command.Produces)
            {
                if (production.InlineEvent is { } inline &&
                    (declarations[inline.Name].Count() > 1 || application.Imports.Any(import => import.Name == inline.Name)))
                {
                    context.Error(DiagnosticCodes.InlineEventCollision, $"Inline event '{inline.Name}' collides with another event declaration or import", production.Location);
                }

                if (mixed && production.For is null)
                {
                    context.Error(DiagnosticCodes.ExplicitProducesTargetsRequired, $"Command '{command.Name}' targets another event source - every production must state 'for' explicitly", production.Location);
                }

                var destination = (production.For as PathExpressionSyntax)?.Path ?? (production.InlineEvent is not null && production.For is null ? identifier : null);
                if (identifier is null || destination != identifier)
                {
                    continue;
                }

                foreach (var mapping in production.Mappings.Where(mapping => mapping.Source is PathExpressionSyntax path && path.Path == identifier))
                {
                    var message = $"Event '{production.Event}' copies command identifier '{identifier}' into payload property '{mapping.Property}' although it already identifies the event source";
                    context.Add(production.InlineEvent is null
                        ? new Diagnostic(DiagnosticSeverity.Information, DiagnosticCodes.EventSourceIdInPayload, message, mapping.Location)
                        : Diagnostic.Warning(DiagnosticCodes.EventSourceIdInPayload, message, mapping.Location));
                }
            }
        }
    }
}
