// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

static class McpReadResults
{
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
}
