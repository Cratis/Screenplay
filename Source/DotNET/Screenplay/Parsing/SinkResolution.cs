// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Settles which read-model lines <c>sink &lt;Name&gt;</c> are the sink directive, once the application's sinks are known.
/// </summary>
/// <remarks>
/// A read-model body line <c>sink Customer</c> was always a property named <c>sink</c>, so the parser keeps reading it as
/// one. It becomes the directive only when <c>Customer</c> is a sink declared at the application root, matched exactly,
/// and the line carries nothing a directive cannot: no escape, collection, optional or modifier. Resolution runs after a
/// document is parsed, for the sinks it declares itself, and again after a folder is merged, for sinks another file
/// declares. A line it has already turned into the directive is no longer a property, so running it twice changes nothing.
/// </remarks>
internal static class SinkResolution
{
    /// <summary>
    /// Resolves the sink directives of every read model against the declared sinks.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to resolve.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <returns>The application with each read model's directives taken out of its properties.</returns>
    internal static ApplicationSyntax Resolve(ApplicationSyntax application, ParserContext context) =>
        Resolve(application, application.Sinks.Select(sink => sink.Name).ToHashSet(StringComparer.Ordinal), context);

    /// <summary>
    /// Resolves the sink directives of every read model against sinks declared anywhere in the application, such as
    /// the other documents of a folder a single document belongs to.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> of one document or of the whole application.</param>
    /// <param name="declared">The names of every declared sink.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <returns>The application with each read model's directives taken out of its properties.</returns>
    internal static ApplicationSyntax Resolve(ApplicationSyntax application, IReadOnlySet<string> declared, ParserContext context)
    {
        var exact = declared;
        if (exact.Count == 0) return application;
        var changed = false;
        var modules = application.Modules.Select(module =>
        {
            var features = Features(module.Features, exact, context, out var moduleChanged);
            changed |= moduleChanged;
            return moduleChanged ? module with { Features = features } : module;
        }).ToArray();
        return changed ? application with { Modules = modules } : application;
    }

    /// <summary>
    /// Determines whether a property is written the way a <c>sink &lt;Name&gt;</c> directive is.
    /// </summary>
    /// <param name="property">The <see cref="PropertySyntax"/> to inspect.</param>
    /// <returns><see langword="true"/> when the line could be the directive.</returns>
    internal static bool HasDirectiveShape(PropertySyntax property) =>
        property.Name == SinkParser.Keyword && !property.NameWasEscaped && !property.IsIdentifier && !property.IsGenerated &&
        !property.IsSubject && !property.IsKey && !property.Type.IsCollection && !property.Type.IsOptional;

    static FeatureSyntax[] Features(IEnumerable<FeatureSyntax> features, IReadOnlySet<string> exact, ParserContext context, out bool changed)
    {
        changed = false;
        var result = new List<FeatureSyntax>();
        foreach (var feature in features)
        {
            var nested = Features(feature.Features, exact, context, out var nestedChanged);
            var slicesChanged = false;
            var slices = feature.Slices.Select(slice =>
            {
                if (slice.ReadModels is null) return slice;
                var readModels = slice.ReadModels.Select(readModel => ReadModel(readModel, exact, context)).ToArray();
                if (readModels.Zip(slice.ReadModels).All(pair => ReferenceEquals(pair.First, pair.Second))) return slice;
                slicesChanged = true;
                return slice with { ReadModels = readModels };
            }).ToArray();
            if (!nestedChanged && !slicesChanged)
            {
                result.Add(feature);
                continue;
            }

            changed = true;
            result.Add(feature with { Features = nestedChanged ? nested : feature.Features, Slices = slicesChanged ? slices : feature.Slices });
        }

        return [.. result];
    }

    static ReadModelSyntax ReadModel(ReadModelSyntax readModel, IReadOnlySet<string> exact, ParserContext context)
    {
        var sink = readModel.Sink;
        var properties = new List<PropertySyntax>();
        var locations = new Dictionary<string, SourceLocation>(readModel.DirectiveLocations);
        var resolved = false;
        foreach (var property in readModel.Properties)
        {
            if (!HasDirectiveShape(property))
            {
                properties.Add(property);
                continue;
            }

            var name = property.Type.Name;
            if (!exact.Contains(name))
            {
                properties.Add(property);
                continue;
            }

            resolved = true;
            if (sink is null)
            {
                sink = name;
                locations[SinkParser.Keyword] = property.Location;
            }
            else if (sink == name)
            {
                context.Warning(DiagnosticCodes.RepeatedSinkDirective, $"Read model '{readModel.Name}' repeats 'sink {name}'; keep one directive.", property.Location);
            }
            else
            {
                context.Error(DiagnosticCodes.ConflictingSinkDirective, $"Read model '{readModel.Name}' names sink '{sink}' and sink '{name}'", property.Location);
            }
        }

        if (!resolved) return readModel;
        if (readModel.IsPassive && readModel.Sink is null)
        {
            context.Error(DiagnosticCodes.PassiveReadModelSink, $"Passive read model '{readModel.Name}' cannot name sink '{sink}'", locations[SinkParser.Keyword]);
        }

        return readModel with { Properties = properties, Sink = sink, DirectiveLocations = locations };
    }
}
