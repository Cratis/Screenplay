// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Rejects personal data as an event source identity, which cannot be encrypted or erased.
/// </summary>
internal static class IdentifierComplianceValidator
{
    /// <summary>Validates command identifiers, command/reaction destinations and event source declarations.</summary>
    public static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        var personal = application.Concepts.Where(concept => concept.AttributeNames.Contains(ConceptAttributeSyntax.Pii))
            .Select(concept => concept.Name).ToHashSet(StringComparer.Ordinal);
        var declaredTriggers = (application.Triggers ?? []).ToLookup(trigger => trigger.Name, StringComparer.Ordinal);
        foreach (var source in application.EventSources)
        {
            if (source.Identifier is { } identifier) ValidateType(identifier, identifier.Location);
        }

        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var command in slice.Commands)
            {
                foreach (var property in command.Properties.Where(property => property.IsIdentifier))
                {
                    ValidateType(property.Type, property.Location);
                }

                foreach (var production in command.Produces.Where(production => declarations.Productions.IsEventProduction(production, slice)))
                {
                    if (production.For is PathExpressionSyntax path && declarations.Property(command.Properties, path.Path, out _) is { IsIdentifier: false } property)
                    {
                        ValidateType(property.Type, path.Location);
                    }
                }
            }

            foreach (var trigger in slice.Reactions.SelectMany(reaction => reaction.Triggers))
            {
                if (trigger.Source is not NamedTriggerSourceSyntax named) continue;
                var shapes = new List<IEnumerable<PropertySyntax>?> { declarations.Event(named.Name, scope)?.Properties };
                shapes.AddRange(declaredTriggers[named.Name].Select(declared => declared.Data
                    .Select(datum => datum.Type is { } type ? new PropertySyntax(datum.Name, type, datum.Location) : null)
                    .OfType<PropertySyntax>()));
                foreach (var production in ReactionProductions.In(trigger).Where(production => declarations.Productions.IsEventProduction(production, slice)))
                {
                    if (production.For is not PathExpressionSyntax path) continue;

                    // Compliance is conservative when event and declared-trigger resolution disagree (#439).
                    var personalType = shapes.Select(properties => declarations.Property(properties, path.Path, out _)?.Type)
                        .FirstOrDefault(type => type is not null && personal.Contains(type.Name));
                    if (personalType is not null) ValidateType(personalType, path.Location);
                }
            }
        }

        void ValidateType(TypeRefSyntax type, SourceLocation location)
        {
            if (personal.Contains(type.Name))
            {
                context.Error(DiagnosticCodes.PiiNotSupportedOnIdentifier, $"Concept '{type.Name}' is @pii and cannot be an event source identifier - use a surrogate Uuid identifier and keep the @pii value as a property", location);
            }
        }
    }
}
