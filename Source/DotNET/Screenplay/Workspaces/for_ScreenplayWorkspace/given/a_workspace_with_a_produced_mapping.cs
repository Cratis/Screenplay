// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.given;

public class a_workspace_with_a_produced_mapping : Specification
{
    protected const string Source =
        """
        // café 🍰: name = name must remain untouched
        module Projects
          feature Registration
            slice StateChange RegisterProject
              description "Registers a project"
              command RegisterProject
                name ProjectName identifier
                displayName ProjectName
                count Int
                produces ProjectRegistered
                  for name
                  name = name  // name = name: café 🍰
              command OtherCommand
                name ProjectName
              event ProjectRegistered
                name ProjectName
              event OtherEvent
                name ProjectName
              specification Registering
                when RegisterProject
                  name = "Old"
                  displayName = "New"
                  count = 42
                then ProjectRegistered
                  name = "New"
        """;

    protected ScreenplayWorkspace _workspace = null!;
    protected WorkspaceDocument _document = null!;
    protected WorkspaceDocument _concepts = null!;
    protected SemanticSlice _slice = null!;
    protected SemanticCommand _command = null!;
    protected SemanticEventContract _event = null!;
    protected UpdateProducedEventMappingSource _operation = null!;

    void Establish() => CreateWorkspace(Source);

    protected void CreateWorkspace(string source)
    {
        var bytes = Encoding.UTF8.Preamble.ToArray().Concat(Encoding.UTF8.GetBytes(source.Replace("\n", "\r\n", StringComparison.Ordinal) + "\n// final comment\n")).ToArray();
        _document = WorkspaceDocument.Create("registration", PortablePlayPath.Parse("Projects/Registration.play"), bytes);
        _concepts = WorkspaceDocument.Create("concepts", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes("concept ProjectName : String\n"));
        _workspace = ScreenplayWorkspace.Create("Projects", [_document, _concepts], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        _slice = _workspace.Compilation.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single();
        _command = _slice.Commands.Single(value => value.Name == "RegisterProject");
        _event = _slice.Events.Single(value => value.Name == "ProjectRegistered");
        _operation = new()
        {
            Command = _command.Id,
            ProducedEvent = _event.Id,
            TargetProperty = _event.Properties.Single().Id,
            ExpectedSourceCommandProperty = _command.Properties.Single(value => value.Name == "name").Id,
            NewSourceCommandProperty = _command.Properties.Single(value => value.Name == "displayName").Id
        };
    }

    protected WorkspaceTransactionRequest Request(params WorkspaceOperation[] operations) => new()
    {
        ExpectedRevision = _workspace.Revision,
        ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
        Operations = [.. operations]
    };

    protected SemanticSpecificationRun Run(ScreenplayWorkspace workspace) =>
        new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(workspace.Compilation.Value!.Model).Plan!, _slice.Specifications.Single().Id);

    protected static ImmutableArray<byte> ExpectedBytes(WorkspaceDocument document) =>
        [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(document.Text.Replace("name = name  //", "name = displayName  //", StringComparison.Ordinal))];
}
#endif
