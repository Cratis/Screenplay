// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff.given;

public class a_semantic_comparison : Specification
{
    internal const string Source = """
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                projectId Uuid identifier
                name String
                produces Registered
                  for projectId
                  name = name
              event Registered
                name String
              specification Registers
                when Register
                  projectId = "11111111-1111-1111-1111-111111111111"
                  name = "First"
                then Registered
                  name = "First"
            slice StateView List
              readmodel Projects
                name String
                projectId Uuid
              query All => Projects optional
                by projectId Uuid
            slice StateView Obsolete
              readmodel Archive
                name String
                projectId Uuid
              query History => Archive optional
                by projectId Uuid
        """;

    internal ScreenplayWorkspace Workspace = null!;
    internal IMcpProposal Proposal = null!;
    internal JsonElement Diff;

    void Establish()
    {
        Workspace = Create(Source);
        Assert.True(Workspace.Compilation.Success, string.Join("; ", Workspace.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    internal static ScreenplayWorkspace Create(string source) => ScreenplayWorkspace.Create("Projects",
        [WorkspaceDocument.Create("application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(source))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    internal void Propose(string source, IEnumerable<SemanticIdentityRename>? renames = null, IEnumerable<SemanticAddress>? retired = null)
    {
        var syntax = new ScreenplayCompiler().Parse(source);
        Assert.True(syntax.Success, string.Join("; ", syntax.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var result = Workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents = [new ReplaceWorkspaceSyntaxDocument(Workspace.Documents[0].Id, syntax.Value!)],
            ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft,
            SemanticRenames = [.. renames ?? []],
            RetiredSemanticAddresses = [.. retired ?? []]
        });
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message).Concat(result.AuthoringDiagnostics.Select(diagnostic => diagnostic.Message))));
        Proposal = new McpAuthoringProposal(Workspace, result, WorkspaceAuthoringValidation.Authoring);
    }

    // Comparison specs exercise immutable snapshots, independently of proposal admission/refactoring.
    internal void CompareSnapshots(string before, string after, Func<SemanticIdentityCatalog, SemanticIdentityCatalog>? migrate = null, ScreenplayWorkspace? seed = null)
    {
        var original = seed ?? Create(before);
        var document = WorkspaceDocument.Create("application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(before));
        Workspace = ScreenplayWorkspace.Create("Projects", [document], original.IdentityCatalog);
        var proposed = WorkspaceDocument.Create("application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(after));
        var candidate = ScreenplayWorkspace.Create("Projects", [proposed], migrate?.Invoke(Workspace.IdentityCatalog) ?? Workspace.IdentityCatalog);
        Proposal = new Comparison(Workspace, candidate);
        Diff = Read();
    }

    internal JsonElement Read(object? arguments = null) => JsonSerializer.SerializeToElement(McpSemanticDiff.Read(Proposal, JsonSerializer.SerializeToElement(arguments ?? new { limit = 200 })), McpJson.Options);

    internal JsonElement[] Items(string section) => [.. Diff.GetProperty("page").GetProperty("items").EnumerateArray().Where(item => item.GetProperty("section").GetString() == section)];

    internal JsonElement Section(string section) => Diff.GetProperty("sections").EnumerateArray().Single(value => value.GetProperty("section").GetString() == section);

    sealed record Comparison(ScreenplayWorkspace Before, ScreenplayWorkspace Workspace) : IMcpProposal
    {
        public WorkspaceWritePlan WritePlan => new() { BeforeRevision = Before.Revision, AfterRevision = Workspace.Revision, BeforeCatalogRevision = Before.IdentityCatalog.Revision, AfterCatalogRevision = Workspace.IdentityCatalog.Revision };
        public bool Accepted => true;
        public string Validation => "Authoring";
    }
}
