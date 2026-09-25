// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_proposing_a_diagnostic_repair.given;

public class a_legacy_validation : Specification
{
    internal const string Source = """
        module Billing
          feature Invoices
            slice StateChange Register
              command Register
                validate csharp
                  ```
                  yield return "invalid";
                  ```
        """;

    protected ScreenplayWorkspace Workspace = null!;
    protected WorkspaceDiagnosticRepair Repair = null!;
    protected WorkspaceAuthoringRequest Request = null!;

    void Establish()
    {
        var document = WorkspaceDocument.Create("billing", PortablePlayPath.Parse("billing.play"), Encoding.UTF8.GetBytes(Source));
        Workspace = ScreenplayWorkspace.Create("Billing", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing")));
        var warning = WorkspaceSyntaxIndex.Create(Workspace).Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyInlineCodeFence);
        Repair = WorkspaceDiagnosticRepairs.Find(Workspace, Workspace.Revision, warning).Single();
        Request = new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = Repair.RequiredFormatting
        };
    }
}
