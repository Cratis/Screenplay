// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static class EventSourceValidator
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var duplicate in application.EventSources.GroupBy(source => source.Name, StringComparer.Ordinal).SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.InvalidEventSourceDeclaration, $"Event source '{duplicate.Name}' has multiple physical declarations.", duplicate.Location);
        }
        foreach (var source in application.EventSources)
        {
            ValidateType(source.Identifier, false, application, context);
            foreach (var duplicate in source.Streams.GroupBy(stream => stream.Name, StringComparer.Ordinal).SelectMany(group => group.Skip(1)))
            {
                context.Error(DiagnosticCodes.InvalidEventSourceDeclaration, $"Stream '{source.Name}.{duplicate.Name}' has multiple declarations under this source.", duplicate.Location);
            }
            foreach (var stream in source.Streams)
            {
                ValidateType(stream.StreamId, true, application, context);
                var parts = stream.StreamIdParts.ToArray();
                if (parts.Length == 0) continue;
                if (parts.Length < 2) context.Error(DiagnosticCodes.InvalidEventSourceDeclaration, "A composite stream id requires at least two named parts.", stream.DirectiveLocations.GetValueOrDefault("streamId", stream.Location));
                foreach (var duplicate in parts.GroupBy(part => part.Name, StringComparer.Ordinal).SelectMany(group => group.Skip(1)))
                {
                    context.Error(DiagnosticCodes.InvalidEventSourceDeclaration, $"Stream id part '{duplicate.Name}' is declared more than once.", duplicate.Location);
                }
                foreach (var part in parts) ValidateType(part.Type, true, application, context);
            }
        }

        var catalog = new EventSourceCatalog(application);
        var values = new ResponseValueTypes(application);
        foreach (var command in declarations.Slices.SelectMany(entry => entry.Slice.Commands))
        {
            if (command.Stream is not { } route || route.PropertyCandidate is not null) continue;
            var resolution = catalog.Resolve(route.EventSource, route.Stream);
            if (resolution.Kind != EventSourceResolutionKind.Unique)
            {
                context.Error(DiagnosticCodes.InvalidCommandStream, $"Stream '{route.EventSource}.{route.Stream}' is {resolution.Kind}; routing requires one physical source and stream.", route.Location);
                continue;
            }
            var source = resolution.Sources[0];
            var stream = resolution.Streams[0];
            if (source.Identifier is { } expected && command.Properties.Where(property => property.IsIdentifier).ToArray() is [var identifier] && declarations.Compatible(identifier.Type, expected) == false)
            {
                context.Warning(DiagnosticCodes.InvalidCommandStream, $"Command identifier '{identifier.Name}' does not have the source's nominal identifier type '{expected.Name}'. The stream does not supply a destination.", identifier.Location);
            }
            if (stream.StreamIdParts.Any())
            {
                ValidateParts(
                    stream,
                    route.StreamIdParts,
                    route.StreamId is not null,
                    route.Location,
                    DiagnosticCodes.InvalidCommandStream,
                    context,
                    (mapping, target) =>
                    {
                        ValidateMapping(command, mapping, target, declarations, values, context);
                        if (mapping.Source is LiteralExpressionSyntax { Value: "" }) context.Error(DiagnosticCodes.InvalidCommandStream, "A composite stream id part cannot be empty text.", mapping.Source.Location);
                    });
                continue;
            }
            if (route.StreamIdParts.Any())
            {
                context.Error(DiagnosticCodes.InvalidCommandStream, "A streamId part block requires a composite stream.", route.Location);
                continue;
            }
            if ((stream.StreamId is null && route.StreamId is not null) || (stream.StreamId is not null && route.StreamId is null))
            {
                context.Error(DiagnosticCodes.InvalidCommandStream, stream.StreamId is null ? "An unkeyed stream cannot take a streamId mapping." : "This keyed stream requires a streamId mapping.", route.Location);
            }
            if (stream.StreamId is { } target && route.StreamId is { } mapping) ValidateMapping(command, mapping, target, declarations, values, context);
        }
    }

    internal static void ValidateParts(EventStreamSyntax stream, IEnumerable<PropertyMappingSyntax> mappings, bool scalar, SourceLocation location, string code, ParserContext context, Action<PropertyMappingSyntax, TypeRefSyntax> validate)
    {
        if (scalar)
        {
            context.Error(code, "A composite stream requires a streamId part block, not a scalar mapping.", location);
            return;
        }
        var parts = stream.StreamIdParts.ToArray();
        var mapped = mappings.ToArray();
        foreach (var part in parts.Where(part => !mapped.Any(mapping => mapping.Property == part.Name)))
        {
            context.Error(code, $"Stream id part '{part.Name}' requires a mapping.", location);
        }
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var mapping in mapped)
        {
            var part = parts.FirstOrDefault(part => part.Name == mapping.Property);
            if (part is null) context.Error(code, $"Unknown stream id part '{mapping.Property}'.", mapping.Location);
            else if (!names.Add(mapping.Property)) context.Error(code, $"Stream id part '{mapping.Property}' is mapped more than once.", mapping.Location);
            else validate(mapping, part.Type);
        }
    }

    static void ValidateType(TypeRefSyntax? type, bool streamId, ApplicationSyntax application, ParserContext context)
    {
        if (type is null) return;
        if (type.IsOptional || type.IsCollection || (application.Types ?? []).Any(composite => composite.Name == type.Name))
        {
            context.Error(DiagnosticCodes.InvalidEventSourceDeclaration, "Identifier and streamId declarations require a nonoptional scalar value type.", type.Location);
            return;
        }
        var concepts = application.Concepts.Where(concept => concept.Name == type.Name).ToArray();
        string? primitive = null;
        if (concepts is [var concept]) primitive = concept.Type;
        else if (concepts.Length == 0 && ConceptSyntax.PrimitiveTypes.Contains(type.Name)) primitive = type.Name;
        if (streamId && primitive is not null && (primitive is not ("String" or "Uuid" or "Int") || (primitive == "Int" && concepts.Length == 0) || concepts.Any(concept => concept.IsEnum)))
        {
            context.Error(DiagnosticCodes.UnsupportedStreamIdType, "Stream ids support text and UUID values and their nominal concepts, plus integer-backed concepts; other types need a future portable formatter.", type.Location);
        }
        if (primitive is null && !application.Imports.Any(import => import.Name == type.Name || import.QualifiedName == type.Name))
        {
            context.Warning(DiagnosticCodes.UnknownType, $"Unknown type '{type.Name}' in event source declaration.", type.Location);
        }
    }

    static void ValidateMapping(CommandSyntax command, PropertyMappingSyntax mapping, TypeRefSyntax target, ConsistencyDeclarations declarations, ResponseValueTypes values, ParserContext context)
    {
        if (mapping.Source is PathExpressionSyntax path)
        {
            var property = declarations.Property(command.Properties, path.Path, out var missing);
            var source = property?.Type;
            if (source is not null)
            {
                var segments = path.Path.Split('.');
                for (var depth = 1; depth < segments.Length; depth++)
                {
                    if (declarations.Property(command.Properties, string.Join('.', segments.Take(depth)), out _) is { } parent)
                    {
                        source = source with { IsCollection = source.IsCollection || parent.Type.IsCollection, IsOptional = source.IsOptional || parent.Type.IsOptional };
                    }
                }
            }
            if (missing || (source is not null && declarations.Compatible(source, target) == false))
            {
                context.Error(DiagnosticCodes.InvalidCommandStream, $"Stream id source '{path.Path}' is absent or incompatible with nominal type '{target.Name}'.", path.Location);
            }
        }
        else if (mapping.Source is LiteralExpressionSyntax && !values.Compatible(mapping.Source, target))
        {
            context.Error(DiagnosticCodes.InvalidCommandStream, $"Stream id value is incompatible with nominal type '{target.Name}'.", mapping.Source.Location);
        }
        else if (mapping.Source is RawExpressionSyntax or ObjectExpressionSyntax or ListExpressionSyntax or LiteralExpressionSyntax { Value: null })
        {
            context.Error(DiagnosticCodes.InvalidCommandStream, "A stream id needs a scalar value source, not a raw expression, collection or absence.", mapping.Source.Location);
        }
    }
}
