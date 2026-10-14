// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming.given;

public class a_workspace_with_assigned_events : Specification
{
    protected const string Concepts = "concept ProjectId : Uuid\nconcept ProjectName : String\n";
    protected const string Events = "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event ProjectRegistered\n        name ProjectName\n";
    protected ScreenplayWorkspace Original = null!;
    protected ImmutableArray<WorkspaceDocument> Split;
    protected byte[] OriginalCatalog = [];

    void Establish()
    {
        var application = ApplicationIdentity.Create("host-projects");
        var document = Document("memory", "memory.play", Concepts + Events);
        var bootstrap = ScreenplayWorkspace.Create(application, "Projects", [document], SemanticIdentityCatalog.Empty(application));
        bootstrap.Compilation.Success.ShouldBeTrue();
        var catalog = SemanticIdentityCatalog.Create(
            application,
            [new("memory", DocumentId.Create("host-document"), SemanticIdentityOrigin.Persisted)],
            [.. bootstrap.IdentityCatalog.Semantics.Select(assignment => assignment with
            {
                Id = SemanticId.Create(assignment.Address.Kind, $"host:{assignment.Id}"),
                Origin = SemanticIdentityOrigin.Persisted
            })],
            [.. bootstrap.IdentityCatalog.EventContracts.Select(assignment => assignment with
            {
                Id = EventContractId.Create(application, $"host:{assignment.Id}"),
                Origin = SemanticIdentityOrigin.Persisted
            })]);
        var assignedDocument = WorkspaceDocument.Create(catalog.ResolveDocument("memory"), "memory", document.Path, document.Bytes.AsSpan());
        Original = ScreenplayWorkspace.Create(application, "Projects", [assignedDocument], catalog);
        Original.Compilation.Success.ShouldBeTrue();
        OriginalCatalog = SemanticIdentityCatalogSerializer.Serialize(Original.IdentityCatalog);
        Split = [Document("application.play", Concepts), Document("Projects/Registration/Registration.play", Events)];
    }

    protected static WorkspaceDocument Document(string path, string text)
    {
        var portable = PortablePlayPath.Parse(path);
        return WorkspaceDocument.Create(ModelRootDocumentKeys.For(portable), portable, Encoding.UTF8.GetBytes(text));
    }

    protected static WorkspaceDocument Document(string key, string path, string text) =>
        WorkspaceDocument.Create(key, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(text));
}
