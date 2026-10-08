// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_renaming_a_source_with_streams : given.an_authoring_connection
{
    internal const string RoutedSource = """
        eventsource Account
          identifier String
          stream Ledger
            streamId String
          stream Profile
        module M
          feature F
            slice StateChange S
              command C
                id String identifier
                period String
                stream Account.Ledger
                  streamId = period
                produces event E
                  for id
              specification X
                when C
                  id = "one"
                  period = "October"
                then E
                  stream Account.Ledger
                    streamId = "October"
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_preserve_catalog_identities_and_change_only_unpinned_stored_names(bool pinned)
    {
        var source = pinned ? RoutedSource.Replace("eventsource Account", "eventsource Account\n  id \"StoredAccount\"", StringComparison.Ordinal) : RoutedSource;
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
        var opened = Open();
        var baseline = Workspace();
        var before = baseline.IdentityCatalog.Semantics.Where(assignment => assignment.Address.Kind is SemanticKind.EventSource or SemanticKind.EventStream).ToArray();
        before.Length.ShouldEqual(3);
        var proposal = Result("propose-rename", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            target = Node("EventSourceSyntax", opened.GetProperty("revision").GetString()!).GetProperty("handle"),
            expectedName = "Account", newName = "Customer", validation = "Executable"
        });
        var candidate = Candidate(proposal);
        candidate.Compilation.Success.ShouldBeTrue();
        var after = candidate.IdentityCatalog.Semantics.Where(assignment => assignment.Address.Kind is SemanticKind.EventSource or SemanticKind.EventStream).ToArray();
        after.Select(assignment => assignment.Id).ShouldContainOnly(before.Select(assignment => assignment.Id));
        after.Single(assignment => assignment.Address.Kind == SemanticKind.EventSource).Address.Name.ShouldEqual("Customer");
        after.Where(assignment => assignment.Address.Kind == SemanticKind.EventStream).All(assignment => assignment.Address.Parts[1].Key == "Customer").ShouldBeTrue();
        var originalModel = baseline.Compilation.Value!.Model;
        var model = candidate.Compilation.Value!.Model;
        model.Application.EventSources.Single().SourceKind.ShouldEqual(pinned ? "StoredAccount" : "Customer");
        (model.Application.EventSources.Single().SourceKind == originalModel.Application.EventSources.Single().SourceKind).ShouldEqual(pinned);
        model.Application.EventSources.Single().Streams.Select(stream => stream.StreamKind).ShouldContainOnly(originalModel.Application.EventSources.Single().Streams.Select(stream => stream.StreamKind));
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
        File.Exists(Path.Combine(RootPath, ".screenplay", "identities.json")).ShouldBeFalse();
    }
}
