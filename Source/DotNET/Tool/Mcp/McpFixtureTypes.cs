// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Tool.Mcp;

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
            if (occurrence.Role == "queryArguments")
            {
                var parameters = query.Filters.Concat(query.By is null ? [] : new[] { query.By }).Where(parameter => parameter.Name == property).ToArray();
                return parameters.Length == 1 ? parameters[0].Type : null;
            }

            var models = index.Resolve(new(query.ReturnType.Name, ["ReadModel", "Type"], target.Scope, query.Location));
            if (models.Length != 1)
            {
                return null;
            }

            target = models[0];
        }

        // A field belongs to this declaration, never to a global dictionary keyed by its spelling.
        var fields = Properties(target.Syntax).Where(field => field.Name == property).ToArray();

        return fields.Length == 1 ? fields[0].Type : null;
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
