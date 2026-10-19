// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy.given;

public class an_identifier_payload_copy_workspace : Specification
{
    protected const string InlineSource = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        produces event Renamed\n          projectId Uuid = projectId\n          name String = \"something\"\n";

    protected const string StandaloneSource = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        produces Renamed\n          for projectId\n          projectId = projectId\n          name = \"something\"\n      event Renamed\n        projectId Uuid\n        name String\n";

    protected ScreenplayWorkspace Workspace = null!;
    protected WorkspaceAuthoringResult Result = null!;

    protected EventSyntax[] Events => [.. WorkspaceSyntaxIndex.Create(Result.Workspace!).Entries.Select(entry => entry.Node).OfType<EventSyntax>()];

    protected EventSyntax Generation(uint generation) => Events.Single(@event => @event.Name == "Renamed" && @event.Generation == generation);

    protected bool StillCopiesTheIdentifier => Result.Workspace!.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.EventSourceIdInPayload);

    protected void Create(string source) => Workspace = ScreenplayWorkspace.Create("Projects",
        [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(source))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    protected WorkspaceSyntaxEntry Copy(ScreenplayWorkspace workspace) => Property(workspace, "projectId");

    protected static WorkspaceSyntaxEntry Property(ScreenplayWorkspace workspace, string name, uint generation = 1, string eventName = "Renamed")
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        return index.Entries.Single(entry => entry.Node is PropertySyntax property && property.Name == name &&
            entry.Parent is { } parent && index.Find(parent)?.Node is EventSyntax @event && @event.Name == eventName && @event.Generation == generation);
    }

    protected WorkspacePropertyRequest Request() => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = Copy(Workspace).Handle,
        ExpectedName = "projectId",
        Action = "remove",
        NewGeneration = true,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };
}
