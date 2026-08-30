// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_operations_are_reordered : given.a_valid_workspace
{
    const string UpdatedRegistration =
        """
        // Registration behavior
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
        """;
    WorkspaceTransactionResult _forward = null!;
    WorkspaceTransactionResult _reverse = null!;

    void Because()
    {
        var move = new MoveWorkspaceDocument
        {
            Document = Concepts.Id,
            Path = PortablePlayPath.Parse("Common/application.play")
        };
        var replace = new ReplaceWorkspaceDocument
        {
            Document = Registration.Id,
            Bytes = Bytes(UpdatedRegistration)
        };
        _forward = Workspace.Propose(Request(move, replace));
        _reverse = Workspace.Propose(Request(replace, move));
    }

    [Fact] void should_accept_both_orders() => new[] { _forward, _reverse }.All(result => result.Success).ShouldBeTrue();
    [Fact] void should_produce_one_workspace_revision() => _reverse.Workspace!.Revision.ShouldEqual(_forward.Workspace!.Revision);
    [Fact] void should_produce_one_catalog_revision() => _reverse.Workspace!.IdentityCatalog.Revision.ShouldEqual(_forward.Workspace!.IdentityCatalog.Revision);
    [Fact] void should_produce_the_same_ordered_document_bytes() => Projection(_reverse.Workspace!).ShouldEqual(Projection(_forward.Workspace!));
    [Fact] void should_order_write_entries_by_document_identity() => _forward.WritePlan!.Entries.Select(entry => entry.Document.ToString()).SequenceEqual(_forward.WritePlan.Entries.Select(entry => entry.Document.ToString()).Order(StringComparer.Ordinal)).ShouldBeTrue();

    static string[] Projection(ScreenplayWorkspace workspace) =>
    [
        .. workspace.Documents.Select(document => $"{document.Id}|{document.StableKey}|{document.Path}|{Convert.ToHexString(document.Bytes.AsSpan())}")
    ];
}
