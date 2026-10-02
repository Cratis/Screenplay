// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions.given;

public class a_command_production : Specification
{
    protected const string Source = """
        concept ProjectName : String
        type Details
          name ProjectName
        module Projects
          feature Registration
            slice StateChange Register
              // Keep the command's explanation.
              command Register
                projectId Uuid identifier
                details Details
                name ProjectName
                produces ProjectRegistered // Keep the production's explanation.
                  for projectId
                  name = name
                  registeredAt = $context.occurred
        """;

    protected static string DestinationSource => Source.Replace("          for projectId\n", string.Empty, StringComparison.Ordinal) + "\n      event ProjectRegistered\n        name ProjectName\n        registeredAt DateTime\n" + VersionAnchor;

    protected const string VersionAnchor = "\n      command Anchor\n        anchorId Uuid identifier\n        produces Anchored\n          for anchorId\n      event Anchored\n";

    protected ScreenplayWorkspace Workspace = null!;
    protected WorkspaceDiagnosticRepair Repair = null!;
    protected WorkspaceAuthoringResult Result = null!;

    protected void Create(string source, params string[] otherSources)
    {
        var sources = new[] { source }.Concat(otherSources);
        var documents = sources.Select((text, index) => WorkspaceDocument.Create($"document-{index}", PortablePlayPath.Parse($"document-{index}.play"), Encoding.UTF8.GetBytes(text))).ToImmutableArray();
        Workspace = ScreenplayWorkspace.Create("Projects", documents, SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
    }

    protected WorkspaceDiagnosticRepair Find(string code) => WorkspaceSyntaxIndex.Create(Workspace).RepairableDiagnostics
        .Where(diagnostic => diagnostic.Code == code)
        .SelectMany(diagnostic => WorkspaceDiagnosticRepairs.Find(Workspace, Workspace.Revision, diagnostic)).First();

    protected bool HasRepair(string code) => WorkspaceSyntaxIndex.Create(Workspace).RepairableDiagnostics
        .Where(diagnostic => diagnostic.Code == code)
        .SelectMany(diagnostic => WorkspaceDiagnosticRepairs.Find(Workspace, Workspace.Revision, diagnostic)).Any();

    protected WorkspaceAuthoringRequest Request() => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };
}
