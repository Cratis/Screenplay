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
        snapshot.SourceRevision,
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
        referenceCoverage = McpReferenceKinds.Coverage,
        snapshot.Compilation.Diagnostics
    };

    internal static object References(McpSnapshot snapshot, JsonElement arguments)
    {
        var index = snapshot.Index;
        var address = McpJson.RequiredString(arguments, "address");
        var kind = McpJson.RequiredString(arguments, "kind");
        var declarations = index.Find(address, kind);
        if (declarations.Length != 1)
        {
            throw new McpFailure("The address and kind must identify exactly one declaration.");
        }

        var resolutions = index.Incoming(declarations[0]).ToArray();
        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            snapshot.Compilation.Diagnostics,
            declaration = Summary(declarations[0]),
            references = resolutions.Where(result => result.Candidates.Length == 1).Select(result => result.Reference),
            ambiguous = resolutions.Where(result => result.Candidates.Length > 1).Select(result => new { reference = result.Reference, candidates = result.Candidates.Select(Summary) }),
            coverage = McpReferenceKinds.Coverage
        };
    }

    internal static object Summary(McpDeclaration declaration) => new { declaration.Kind, declaration.Name, declaration.Address, declaration.Scope, declaration.Location, declaration.Locations, declaration.Description, declaration.IsImplicit };

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
