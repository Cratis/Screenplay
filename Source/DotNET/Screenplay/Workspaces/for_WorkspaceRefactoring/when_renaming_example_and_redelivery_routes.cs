// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_example_and_redelivery_routes
{
    const string Source = """
        eventsource Account
          identifier String
          stream Ledger
        module M
          feature F
            slice Automation S
              event Recorded
              reaction Observer
                when Recorded
              example Prior : Recorded
                for "account"
                stream Account.Ledger
              specification Recovery
                given Prior
                when redelivered Recorded to Observer
                  stream Account.Ledger
                then no events
        """;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_repair_the_authored_route_references(bool renameSource)
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source));
        var workspace = ScreenplayWorkspace.Create("A", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => renameSource ? entry.Node is EventSourceSyntax : entry.Node is EventStreamSyntax);
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = renameSource ? "Account" : "Ledger",
            NewName = renameSource ? "Customer" : "History",
            Formatting = WorkspaceAuthoringFormatting.PreserveTrivia
        });
        result.Conflicts.ShouldBeEmpty();
        result.Accepted.ShouldBeTrue();
        var routes = WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node).OfType<SpecificationStreamSyntax>().ToArray();
        routes.Length.ShouldEqual(2);
        var expected = renameSource ? "Customer.Ledger" : "Account.History";
        routes.All(route => $"{route.EventSource}.{route.Stream}" == expected).ShouldBeTrue();
    }
}
