// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

static class McpReadResults
{
    internal static object References(McpSnapshot snapshot, JsonElement arguments)
    {
        var index = snapshot.Index;
        var declaration = McpDeclarationDetails.Target(snapshot, arguments);
        var resolutions = index.Incoming(declaration).Select(result => new McpReferenceEdge(result.Reference, result.Candidates)).ToArray();
        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            snapshot.Compilation.Diagnostics,
            declaration = Summary(declaration),
            references = resolutions.Where(result => result.Resolution == "resolved").Select(result => result.Reference),
            ambiguous = resolutions.Where(result => result.Resolution == "ambiguous").Select(result => new { reference = result.Reference, candidates = result.Targets.Select(Summary) }),
            incomplete = resolutions.Where(result => result.Resolution == "incomplete").Select(result => new { reference = result.Reference, candidates = result.Targets.Select(Summary) }),
            coverage = McpReferenceKinds.Coverage
        };
    }

    internal static object Summary(McpDeclaration declaration) => new { declaration.Kind, declaration.Name, declaration.Address, declaration.Scope, declaration.Location, declaration.Locations, declaration.Description, declaration.IsImplicit, declaration.Case };
}
