// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpReadResults
{
    internal static object Describe(McpSnapshot snapshot, int fileCount) => new
    {
        snapshot.Compilation.Success,
        fileCount,
        modules = snapshot.Compilation.Value?.Modules.Select(module => new
        {
            module.Name,
            module.Description,
            module.Location,
            features = Features(module.Features)
        }),
        declarations = snapshot.Index.Declarations.Select(Summary),
        unresolvedOrAmbiguous = snapshot.Index.References.Select(reference => new { reference, candidates = snapshot.Index.Resolve(reference).Select(Summary).ToArray() }).Where(result => result.candidates.Length != 1),
        referenceCoverage = "Explicit declaration references only; not code, property paths, imports, profile settings, or external host registrations.",
        snapshot.Compilation.Diagnostics
    };

    internal static object References(McpSyntaxIndex index, JsonElement arguments)
    {
        var address = McpJson.RequiredString(arguments, "address");
        var kind = McpJson.RequiredString(arguments, "kind");
        var declarations = index.Declarations.Where(declaration => declaration.Address == address && declaration.Kind == kind).ToArray();
        if (declarations.Length != 1)
        {
            throw new McpFailure("The address and kind must identify exactly one declaration.");
        }

        var resolutions = index.References.Select(reference => new { reference, candidates = index.Resolve(reference) }).ToArray();
        return new
        {
            declaration = Summary(declarations[0]),
            references = resolutions.Where(result => result.candidates.Length == 1 && result.candidates[0] == declarations[0]).Select(result => result.reference),
            ambiguous = resolutions.Where(result => result.candidates.Length > 1 && result.candidates.Contains(declarations[0])).Select(result => new { result.reference, candidates = result.candidates.Select(Summary) }),
            coverage = "Explicit declaration references only; not code, property paths, imports, profile settings, or external host registrations."
        };
    }

    static object Summary(McpDeclaration declaration) => new { declaration.Kind, declaration.Name, declaration.Address, declaration.Location, declaration.Description };

    static IEnumerable<object> Features(IEnumerable<FeatureSyntax> features) => features.Select(feature => new
    {
        feature.Name,
        feature.Description,
        feature.Location,
        features = Features(feature.Features),
        slices = feature.Slices.Select(slice => new
        {
            slice.Name,
            slice.Description,
            slice.Location,
            commands = slice.Commands.Select(command => new { command.Name, command.Description, produces = command.Produces.Select(produces => produces.Event) }),
            events = slice.Events.Select(@event => @event.Name),
            specifications = slice.Specifications.Select(specification => new
            {
                specification.Name,
                command = specification.When?.CommandType,
                givenEvents = specification.Given.Count(),
                expectedEvents = specification.ThenEvents.Count(),
                expectedErrors = specification.ThenErrors.Count(),
                expectedReadModels = specification.ThenReadModels?.Count() ?? 0,
                expectedQueries = specification.ThenQueries.Count()
            })
        })
    });
}
