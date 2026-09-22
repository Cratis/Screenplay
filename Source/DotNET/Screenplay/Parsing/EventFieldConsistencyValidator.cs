// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks event assignment targets against the event actually referenced in scope.
/// </summary>
internal static class EventFieldConsistencyValidator
{
    /// <summary>
    /// Validates producer mappings and specification event assignments.
    /// </summary>
    /// <param name="declarations">The application declarations.</param>
    /// <param name="context">The diagnostic sink.</param>
    public static void Validate(ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            var producers = slice.Commands.SelectMany(command => command.Produces)
                .Concat(slice.Reactions.SelectMany(reaction => reaction.Triggers).SelectMany(trigger => trigger.Produces ?? []));
            foreach (var producer in producers)
            {
                ValidateAssignments(producer.Event, producer.Mappings, scope, declarations, context);
            }

            foreach (var append in slice.Captures.SelectMany(capture => capture.Appends
                .Concat(capture.Children.SelectMany(children => children.Appends))
                .Concat(capture.Nested.SelectMany(nested => nested.Appends))))
            {
                ValidateAssignments(append.Event, append.Mappings, scope, declarations, context);
            }

            foreach (var step in slice.Specifications.SelectMany(specification => specification.Given.Concat(specification.ThenEvents)))
            {
                ValidateAssignments(step.EventType, step.Values, scope, declarations, context);
            }
        }
    }

    static void ValidateAssignments(string eventName, IEnumerable<PropertyMappingSyntax> assignments, DeclarationScope scope, ConsistencyDeclarations declarations, ParserContext context)
    {
        var properties = declarations.Event(eventName, scope)?.Properties;
        foreach (var assignment in assignments)
        {
            declarations.Property(properties, assignment.Property, out var missing);
            if (missing)
            {
                context.Error(
                    DiagnosticCodes.UnknownEventField,
                    $"Event '{eventName}' declares no field '{assignment.Property}'",
                    assignment.Location);
            }
        }
    }
}
