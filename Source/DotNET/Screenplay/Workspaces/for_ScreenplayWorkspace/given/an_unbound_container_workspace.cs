// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.given;

public class an_unbound_container_workspace : Specification
{
    protected const string Source = """
        module Sales
          feature Orders
            feature Drafts
              slice StateChange PlaceOrder
                command PlaceOrder
                  orderId Missing identifier
                  produces OrderPlaced
                event OrderPlaced
        """;

    protected static readonly ApplicationIdentity Application = ApplicationIdentity.Create("Acme");
    protected ScreenplayWorkspace Workspace = null!;

    void Establish() => Workspace = Create(Source);

    protected static WorkspaceDocument Document(string source, string key = "application") => WorkspaceDocument.Create(key, PortablePlayPath.Parse($"{key}.play"), Encoding.UTF8.GetBytes(source));

    protected static ScreenplayWorkspace Create(string source, SemanticIdentityCatalog? catalog = null) => ScreenplayWorkspace.Create("Acme", [Document(source)], catalog ?? SemanticIdentityCatalog.Create(Application, [new("application", DocumentId.Create("application"), SemanticIdentityOrigin.LegacyBootstrap)], [], []));

    protected static WorkspaceAuthoringResult Replace(ScreenplayWorkspace workspace, string source) => workspace.ProposeAuthoring(new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Documents = [new ReplaceWorkspaceSyntaxDocument(workspace.Documents.Single().Id, new ScreenplayCompiler().Compile(source).Value!)]
    });
}
