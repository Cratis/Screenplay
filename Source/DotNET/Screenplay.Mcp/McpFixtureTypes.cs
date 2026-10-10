// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp;

static class McpFixtureTypes
{
    internal static TypeRefSyntax? For(McpSyntaxIndex index, McpFixtureOccurrence occurrence, string property)
    {
        var candidates = index.Resolve(occurrence.Reference);
        if (candidates.Length != 1)
        {
            return null;
        }

        var target = candidates[0];
        if (target.Syntax is QuerySyntax query)
        {
            if (occurrence.Role == "queryArguments" || occurrence.Role == "whenQueryArguments")
            {
                var parameters = query.Filters.Concat(query.By is null ? [] : new[] { query.By }).Concat(query.ByParts).Where(parameter => parameter.Name == property).ToArray();
                return parameters.Length == 1 ? parameters[0].Type : null;
            }

            var models = index.Resolve(new(query.ReturnType.Name, ["ReadModel", "Type"], target.Scope, query.Location));
            if (models.Length != 1)
            {
                return null;
            }

            target = models[0];
        }

        if (occurrence.Role == "thenReturns" && target.Syntax is CommandSyntax command)
        {
            var source = command.Response switch
            {
                ScalarCommandResponseSyntax scalar when property == "returns" => scalar.Source.Property,
                RecordCommandResponseSyntax record => Source(record, property),
                _ => null
            };
            var sources = command.Properties.Where(field => field.Name == source).ToArray();
            return sources.Length == 1 ? sources[0].Type : null;
        }

        if (occurrence.Role == "thenAbsentReadModel" && property == "for" && target.Syntax is ReadModelSyntax model)
        {
            var explicitKeys = model.Properties.Where(field => field.IsKey).ToArray();
            if (explicitKeys.Length > 0) return explicitKeys.Length == 1 ? explicitKeys[0].Type : null;
            var keyed = index.Declarations.Where(declaration => declaration.Syntax is QuerySyntax { By: not null } query && !query.ReturnType.IsCollection &&
                    index.Resolve(new(query.ReturnType.Name, ["ReadModel"], declaration.Scope, declaration.Location)) is [var resolved] && ReferenceEquals(resolved.Syntax, model))
                .Select(declaration => ((QuerySyntax)declaration.Syntax).By!.Name).Distinct(StringComparer.Ordinal).ToArray();
            var keyFields = keyed.Length == 1 ? model.Properties.Where(field => field.Name == keyed[0]).ToArray() : [];

            return keyFields.Length == 1 ? keyFields[0].Type : null;
        }

        // A field belongs to this declaration, never to a global dictionary keyed by its spelling.
        var fields = Properties(target.Syntax).Where(field => field.Name == property).ToArray();

        return fields.Length == 1 ? fields[0].Type : null;
    }

    static string? Source(RecordCommandResponseSyntax response, string property)
    {
        var fields = response.Fields.Where(field => field.Name == property).ToArray();
        return fields.Length == 1 ? fields[0].Source.Property : null;
    }

    static IEnumerable<PropertySyntax> Properties(SyntaxNode node) => node switch
    {
        CommandSyntax command => command.Properties,
        EventSyntax @event => @event.Properties,
        ReadModelSyntax model => model.Properties,
        TypeSyntax type => type.Properties,
        _ => []
    };
}
