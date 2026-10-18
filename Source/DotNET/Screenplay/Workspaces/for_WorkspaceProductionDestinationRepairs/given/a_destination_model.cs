// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceProductionDestinationRepairs.given;

public class a_destination_model
{
    internal const string Source = """
        module M
          feature F
            slice StateChange S
              command Create
                id Uuid identifier
                otherId Uuid
                name String
                // keep production comment
                produces event Created
                  name String = name
                produces Existing
                  name = name
                produces when name == "again"
                  Existing
                    for id
                    name = name
              event Existing
                name String
        """;

    internal static ScreenplayWorkspace Create(string? source = null) => ScreenplayWorkspace.Create(ApplicationIdentity.Create("Destinations"), "Destinations",
        [WorkspaceDocument.Create("model", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(source ?? Source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Destinations")));

    internal static WorkspaceAuthoringRequest Request(ScreenplayWorkspace workspace) => new()
    {
        ExpectedRevision = workspace.Revision, ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, Validation = WorkspaceAuthoringValidation.Executable
    };

    internal static ProductionDestinationIntent Intent(ImmutableArray<ProductionDestinationDescriptor> rows) => new(1,
        [.. rows.Select(row => new ProductionDestinationSelection(row.Subject, row.ExistingFor ?? new PathExpressionSyntax("id", row.Diagnostics[0].Location), row.Route!))]);

    internal static WorkspaceAuthoringResult Propose(ScreenplayWorkspace workspace, ProductionDestinationIntent? intent = null, WorkspaceAuthoringRequest? request = null)
    {
        var rows = WorkspaceProductionDestinationRepairs.Discover(workspace);
        rows.Length.ShouldEqual(3);
        return WorkspaceProductionDestinationRepairs.Propose(workspace, rows[0].Command, intent ?? Intent(rows), request ?? Request(workspace));
    }
}
