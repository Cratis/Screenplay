// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff.given;

public class an_export_with_a_different_application_identity : comparison_sources
{
    void Establish() => Export = two_snapshots.Export(ScreenplayWorkspace.Create("Other", Root.Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Other"))));
}
