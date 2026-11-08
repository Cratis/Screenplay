// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Contracts;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool;

// Renames a declaration through the MCP server's own propose-rename and apply, in process, so the rename keeps the
// declaration's semantic identity and .screenplay/identities.json is written exactly as an MCP rename writes it.
static class RenameCommand
{
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var arguments = CommandArguments.Parse(args, CliCommandCatalog.Rename);
        if (arguments is null || arguments.Positional.Count is < 2 or > 3)
        {
            error.WriteLine($"Usage: {CliCommandCatalog.Rename.Usage}");
            return 2;
        }

        var root = arguments.Positional.Count == 3 ? arguments.Positional[2] : Directory.GetCurrentDirectory();
        if (!Directory.Exists(root))
        {
            error.WriteLine($"'{root}' must be an existing model folder.");
            return 2;
        }

        try
        {
            return Rename(ModelSource.RealPath(root), arguments.Positional[0], arguments.Positional[1], arguments.Option(CliCommandCatalog.Kind), output, error);
        }
        catch (Exception exception) when (ModelSource.IsReadFailure(exception))
        {
            error.WriteLine($"Could not rename '{arguments.Positional[0]}': {exception.Message}");
            return 2;
        }
    }

    static int Rename(string root, string address, string newName, string? kind, TextWriter output, TextWriter error)
    {
        var workspaces = new McpWorkspaces(new McpRoot(root));
        _ = workspaces.Open(Arguments(new { }));
        var workspace = workspaces.ReadComparisonWorkspace().Workspace;
        var matches = RenameTargets.Find(workspace, address, kind);
        if (matches.Length != 1)
        {
            error.WriteLine(matches.Length == 0
                ? $"'{address}' does not name a declaration{(kind is null ? string.Empty : $" of kind {kind}")}."
                : $"'{address}' is ambiguous: {string.Join(", ", matches.Select(match => match.Address!.Kind))}. Pass {CliCommandCatalog.Kind.Name}.");
            return 2;
        }

        var revisions = new { expectedRevision = workspace.Revision.ToString(), expectedCatalogRevision = workspace.IdentityCatalog.Revision.ToString() };
        var proposal = Result(workspaces.Rename(Arguments(new
        {
            revisions.expectedRevision,
            revisions.expectedCatalogRevision,
            target = JsonSerializer.SerializeToElement(McpAstHandles.Describe(matches[0].Handle), McpJson.Options),
            expectedName = matches[0].Address!.Name,
            newName
        })));
        if (!proposal.TryGetProperty("proposalId", out var proposalId) || proposalId.ValueKind != JsonValueKind.String)
        {
            error.WriteLine($"The rename was refused: {proposal.GetRawText()}");
            return 1;
        }

        _ = Result(workspaces.Apply(Arguments(new { revisions.expectedRevision, revisions.expectedCatalogRevision, proposalId = proposalId.GetString() })));
        var renamed = workspaces.ReadComparisonWorkspace().Workspace;
        output.WriteLine($"Renamed {matches[0].Address!.Kind} '{address}' to '{newName}'; semantic identity {matches[0].SemanticId?.ToString() ?? "unassigned"} kept, catalog revision {renamed.IdentityCatalog.Revision}.");
        return 0;
    }

    static JsonElement Arguments(object value) => JsonSerializer.SerializeToElement(value, McpJson.Options);

    // A tool result carries its structured JSON; an error result becomes the failure it describes.
    static JsonElement Result(object toolResult)
    {
        var result = JsonSerializer.SerializeToElement(toolResult, McpJson.Options);
        var structured = result.TryGetProperty("structuredContent", out var content) ? content : result;
        if (result.TryGetProperty("isError", out var isError) && isError.ValueKind == JsonValueKind.True)
        {
            throw new McpFailure(structured.GetRawText());
        }

        return structured;
    }
}

// Resolves a dotted Module[.Feature...][.Slice][.Declaration] address to the syntax entries that declare it.
static class RenameTargets
{
    internal static WorkspaceSyntaxEntry[] Find(ScreenplayWorkspace workspace, string address, string? kind) =>
    [
        .. WorkspaceSyntaxIndex.Create(workspace).Entries.Where(entry =>
            entry.Address is { } candidate &&
            candidate.Parts.All(part => part.Kind is SemanticAddressPartKind.Application or SemanticAddressPartKind.Module or
                SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice or SemanticAddressPartKind.Declaration) &&
            string.Join('.', candidate.Parts.Skip(1).Select(part => part.Key)) == address &&
            (kind is null || string.Equals(candidate.Kind.ToString(), kind, StringComparison.OrdinalIgnoreCase)))
    ];
}
