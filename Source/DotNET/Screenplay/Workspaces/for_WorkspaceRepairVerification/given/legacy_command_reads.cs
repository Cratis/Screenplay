// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRepairVerification.given;

public static class legacy_command_reads
{
    public const string Source = """
        module M
          feature F
            slice StateChange Write
              event Recorded
                value String
              event Removed
              command Update
                id String identifier
                value String
                reads View by id
                validate
                  require View.value != ""
                produces Recorded
                  for id
                  value = View.value
            slice StateView Lookup
              readmodel View
                id String key
                value String
              projection Build => View
                from Recorded
                  id = $eventSourceId
                  value = value
                remove with Removed
        """;

    public static ScreenplayWorkspace Create(params (string Path, string Source)[] sources) => ScreenplayWorkspace.Create("Reads",
        [.. sources.Select(source => WorkspaceDocument.Create(source.Path, PortablePlayPath.Parse(source.Path), Encoding.UTF8.GetBytes(source.Source)))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Reads")));

    public static WorkspaceAuthoringRequest Request(ScreenplayWorkspace workspace) => new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Validation = WorkspaceAuthoringValidation.Authoring
    };
}
