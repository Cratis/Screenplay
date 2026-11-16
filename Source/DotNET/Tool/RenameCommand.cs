// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Contracts;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
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
            return Rename(ModelSource.RealPath(root), new(arguments.Positional[0], arguments.Positional[1], arguments.Option(CliCommandCatalog.Kind)), output, error);
        }
        catch (Exception exception) when (ModelSource.IsReadFailure(exception))
        {
            error.WriteLine($"Could not rename '{arguments.Positional[0]}': {exception.Message}");
            return 2;
        }
    }

    static int Rename(string root, RenameInput input, TextWriter output, TextWriter error)
    {
        var workspaces = new McpWorkspaces(new McpRoot(root));
        _ = workspaces.Open(Arguments(new { }));
        var workspace = workspaces.ReadComparisonWorkspace().Workspace;
        var matches = RenameTargets.Find(workspace, input.Address, input.Kind);
        if (matches.Length != 1)
        {
            error.WriteLine(NoSingleMatch(matches, input));
            return 2;
        }

        var revisions = new { expectedRevision = workspace.Revision.ToString(), expectedCatalogRevision = workspace.IdentityCatalog.Revision.ToString() };
        var proposal = Propose(workspaces, workspace, matches[0], input.NewName);
        if (!proposal.TryGetProperty("proposalId", out var proposalId) || proposalId.ValueKind != JsonValueKind.String)
        {
            error.WriteLine($"The rename was refused: {proposal.GetRawText()}");
            return 1;
        }

        _ = Result(workspaces.Apply(Arguments(new { revisions.expectedRevision, revisions.expectedCatalogRevision, proposalId = proposalId.GetString() })));
        var renamed = workspaces.ReadComparisonWorkspace().Workspace;
        output.WriteLine($"Renamed {Kind(matches[0])} '{input.Address}' to '{input.NewName}'; semantic identity {matches[0].SemanticId?.ToString() ?? "unassigned"} kept, catalog revision {renamed.IdentityCatalog.Revision}.");
        return 0;
    }

    static string NoSingleMatch(WorkspaceSyntaxEntry[] matches, RenameInput input) => matches.Length == 0
        ? $"'{input.Address}' does not name a declaration{(input.Kind is null ? string.Empty : $" of kind {input.Kind}")}."
        : $"'{input.Address}' is ambiguous: {string.Join(", ", matches.Select(Kind))}. Pass {CliCommandCatalog.Kind.Name}.";

    static JsonElement Propose(McpWorkspaces workspaces, ScreenplayWorkspace workspace, WorkspaceSyntaxEntry target, string newName) =>
        Result(workspaces.Rename(Arguments(new
        {
            expectedRevision = workspace.Revision.ToString(),
            expectedCatalogRevision = workspace.IdentityCatalog.Revision.ToString(),
            target = JsonSerializer.SerializeToElement(McpAstHandles.Describe(target.Handle), McpJson.Options),
            expectedName = target.Node is ProvideSyntax provided ? provided.Name : target.Address!.Name,
            newName
        })));

    static string Kind(WorkspaceSyntaxEntry entry) => entry.Address?.Kind.ToString() ?? "Provide";

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

// What the user asked to rename: the dotted address, the new name and an optional kind to disambiguate.
sealed record RenameInput(string Address, string NewName, string? Kind);

// Resolves a dotted Module[.Feature...][.Slice][.Declaration] address to the syntax entries that declare it.
static class RenameTargets
{
    internal static WorkspaceSyntaxEntry[] Find(ScreenplayWorkspace workspace, string address, string? kind)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        return
        [
            .. index.Entries.Where(entry =>
                entry.Address is { } candidate &&
                candidate.Parts.All(part => part.Kind is SemanticAddressPartKind.Application or SemanticAddressPartKind.Module or
                    SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice or SemanticAddressPartKind.Declaration) &&
                string.Join('.', candidate.Parts.Skip(1).Select(part => part.Key)) == address &&
                (kind is null || string.Equals(candidate.Kind.ToString(), kind, StringComparison.OrdinalIgnoreCase))),
            .. index.Entries.Where(entry => entry.Node is ProvideSyntax && (kind is null || string.Equals(kind, "Provide", StringComparison.OrdinalIgnoreCase)) && ProvideAddress(index, entry) == address)
        ];
    }

    // A provided value has no semantic identity; it is addressed by the Module[.Feature][.Slice] it is declared at plus its name (just the name at the application level).
    static string ProvideAddress(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry provide)
    {
        var parts = new List<string> { ((ProvideSyntax)provide.Node).Name };
        for (var current = provide.Parent is { } parent ? index.Find(parent) : null; current is not null; current = current.Parent is { } next ? index.Find(next) : null)
        {
            switch (current.Node)
            {
                case ModuleSyntax module:
                    parts.Insert(0, module.Name);
                    break;
                case FeatureSyntax feature:
                    parts.Insert(0, feature.Name);
                    break;
                case SliceSyntax slice:
                    parts.Insert(0, slice.Name);
                    break;
            }
        }

        return string.Join('.', parts);
    }
}
