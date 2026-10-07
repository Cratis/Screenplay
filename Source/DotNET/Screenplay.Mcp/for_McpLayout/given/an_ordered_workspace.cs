// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpLayout.given;

public class an_ordered_workspace : Specification
{
    protected const string Source = """
        module Zulu
          feature Zulu
            slice StateView Zulu
            slice StateView Alpha
            feature Zulu
              slice StateView View
            feature Alpha
              slice StateView View
          feature Alpha
            slice StateView View
        module Alpha
          feature View
            slice StateView View
        """;

    protected ScreenplayWorkspace Before = null!;
    protected ScreenplayWorkspace After = null!;
    protected bool KeepsOrder;

    void Establish() => Before = Workspace(("application.play", Source));

    protected static ScreenplayWorkspace Workspace(params (string Path, string Source)[] documents) => ScreenplayWorkspace.Create("Example",
        [.. documents.Select(document => WorkspaceDocument.Create(McpDocumentKeys.For(document.Path), PortablePlayPath.Parse(document.Path), Encoding.UTF8.GetBytes(document.Source)))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Example")));
}
