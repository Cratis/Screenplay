// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRepairVerification;

public class when_completing_a_partial_query_key : Specification
{
    const string Source = "module M\n  feature F\n    slice StateView S\n      readmodel Row\n        resourceId String key\n        period Int key\n        label String\n      query Find => Row optional\n        by resourceId String\n";

    [Theory]
    [InlineData("resourceId String")]
    [InlineData("label String")]
    [InlineData("resourceId Int")]
    void should_offer_no_query_repair(string parameter)
    {
        var document = WorkspaceDocument.Create(
            "source",
            PortablePlayPath.Parse("source.play"),
            Encoding.UTF8.GetBytes(Source.Replace("by resourceId String", "by " + parameter, StringComparison.Ordinal)));
        var workspace = ScreenplayWorkspace.Create("Keys", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Keys")));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var diagnostic = index.RepairableDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.IncompleteReadModelKey);
        WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic).ShouldBeEmpty();
    }
}
