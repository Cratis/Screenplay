// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Warns when a projection or reducer copies a value whose concept is marked <c>secret</c> into a read-model property:
/// the value then becomes part of the read model and of every query result over it.
/// </summary>
internal static class SecretProjectionValidator
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        var secrets = application.Concepts
            .Where(concept => concept.AttributeNames.Contains(ConceptAttributeSyntax.Sensitive))
            .Select(concept => concept.Name)
            .ToHashSet(StringComparer.Ordinal);
        if (secrets.Count == 0) return;

        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var projection in slice.Projections)
            {
                new SecretProjectionMappings(projection, secrets, declarations, scope, context).Validate();
            }

            foreach (var reducer in slice.Reducers ?? [])
            {
                ValidateReducer(reducer, secrets, declarations.ViewProperties(reducer.ReadModel, scope), context);
            }
        }
    }

    internal static string Message(string owner, string name, string concept, string property) =>
        $"{owner} '{name}' copies secret concept '{concept}' into read model property '{property}' - the value becomes part of the read model and its query results";

    // A reducer's rules are code the compiler does not read, so every read-model property it builds that is typed with a
    // secret concept is a value the reducer copies there.
    static void ValidateReducer(ReducerSyntax reducer, HashSet<string> secrets, IEnumerable<PropertySyntax>? properties, ParserContext context)
    {
        foreach (var property in (properties ?? []).Where(property => secrets.Contains(property.Type.Name)))
        {
            context.Warning(
                DiagnosticCodes.SecretConceptProjected,
                Message("Reducer", reducer.Name, property.Type.Name, $"{reducer.ReadModel}.{property.Name}"),
                reducer.Location);
        }
    }
}
