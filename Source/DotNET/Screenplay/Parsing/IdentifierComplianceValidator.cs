// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Rejects personal data and operational secrets as event source and stream identities, which cannot be encrypted or erased.
/// </summary>
internal static class IdentifierComplianceValidator
{
    /// <summary>Validates command identifiers, command/reaction destinations and event source declarations.</summary>
    public static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        var personal = application.Concepts.Where(concept => concept.AttributeNames.Contains(ConceptAttributeSyntax.Pii))
            .Select(concept => concept.Name).ToHashSet(StringComparer.Ordinal);
        var sensitive = application.Concepts.Where(concept => concept.AttributeNames.Contains(ConceptAttributeSyntax.Sensitive))
            .Select(concept => concept.Name).ToHashSet(StringComparer.Ordinal);
        var declaredTriggers = (application.Triggers ?? []).ToLookup(trigger => trigger.Name, StringComparer.Ordinal);
        var imports = application.Imports.Select(import => import.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var source in application.EventSources)
        {
            if (source.Identifier is { } identifier) ValidateType(identifier, identifier.Location);
            foreach (var stream in source.Streams)
            {
                if (stream.StreamId is { } streamId) ValidateType(streamId, streamId.Location, "a stream id");
                foreach (var part in stream.StreamIdParts) ValidateType(part.Type, part.Type.Location, $"a stream id part '{part.Name}'");
            }
        }
        var catalog = new EventSourceCatalog(application);

        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var command in slice.Commands)
            {
                foreach (var property in command.Properties.Where(property => property.IsIdentifier))
                {
                    ValidateType(property.Type, property.Location);
                }

                var routes = (command.Stream is { } selected ? new[] { selected }.Concat(command.StreamCandidates) : command.StreamCandidates)
                    .Concat(command.Produces.Select(produced => produced.Stream).OfType<CommandStreamSyntax>())
                    .Where(route => route.PropertyCandidate is null);
                foreach (var route in routes)
                {
                    var resolution = catalog.Resolve(route.EventSource, route.Stream);
                    var stream = resolution.Kind == EventSourceResolutionKind.Unique ? resolution.Streams[0] : null;
                    if (route.StreamId is { } mapping) ValidateMapping(command, mapping, stream?.StreamId);
                    foreach (var part in route.StreamIdParts)
                    {
                        ValidateMapping(command, part, stream?.StreamIdParts.FirstOrDefault(declared => declared.Name == part.Property)?.Type);
                    }
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
                var resolvedEvent = declarations.Event(named.Name, scope);
                var shapes = new List<IEnumerable<PropertySyntax>?>
                {
                    resolvedEvent?.Properties,
                    trigger.Data.Select(datum => datum.Type is { } type ? new PropertySyntax(datum.Name, type, datum.Location) : null)
                        .OfType<PropertySyntax>()
                };
                if (resolvedEvent is null && !imports.Contains(named.Name))
                {
                    shapes.AddRange(declaredTriggers[named.Name].Select(declared => declared.Data
                        .Select(datum => datum.Type is { } type ? new PropertySyntax(datum.Name, type, datum.Location) : null)
                        .OfType<PropertySyntax>()));
                }
                foreach (var production in ReactionProductions.In(trigger).Where(production => declarations.Productions.IsEventProduction(production, slice)))
                {
                    if (production.For is not PathExpressionSyntax path) continue;

                    // Check clause-local types alongside the occurrence selected by event-first reaction resolution.
                    var protectedType = shapes.Select(properties => declarations.Property(properties, path.Path, out _)?.Type)
                        .FirstOrDefault(type => type is not null && (personal.Contains(type.Name) || sensitive.Contains(type.Name)));
                    if (protectedType is not null) ValidateType(protectedType, path.Location);
                }
            }
        }

        void ValidateMapping(CommandSyntax command, PropertyMappingSyntax mapping, TypeRefSyntax? target)
        {
            if (mapping.Source is not PathExpressionSyntax path || declarations.Property(command.Properties, path.Path, out _) is not { } property) return;

            // The same protected concept has already been reported at its resolved declaration.
            if (property.Type.Name == target?.Name && (personal.Contains(property.Type.Name) || sensitive.Contains(property.Type.Name))) return;
            ValidateType(property.Type, mapping.SourceLocation ?? path.Location, "a stream id route mapping");
        }

        void ValidateType(TypeRefSyntax type, SourceLocation location, string position = "an event source identifier")
        {
            if (personal.Contains(type.Name) || sensitive.Contains(type.Name))
            {
                var attribute = personal.Contains(type.Name) ? "pii" : "secret";
                context.Error(DiagnosticCodes.PiiNotSupportedOnIdentifier, $"Concept '{type.Name}' is {attribute} and cannot be {position} - use a surrogate Uuid identifier and keep the {attribute} value as a property", location);
            }
        }
    }
}
