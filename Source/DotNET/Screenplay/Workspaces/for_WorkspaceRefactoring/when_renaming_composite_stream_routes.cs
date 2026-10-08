// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_composite_stream_routes
{
    const string Source = """
        eventsource Account
          id "StoredAccount"
          identifier String
          stream Profile
            id "StoredProfile"
            streamId
              Account String
              Profile String
        module M
          feature F
            slice StateChange S
              event E
              command C
                id String identifier
                stream Account.Profile // route Account.Profile
                  streamId
                    Profile = "Account.Profile"
                    Account = id
                produces E
                  for id
              specification Routed
                given E
                  for "id"
                  stream Account.Profile
                    streamId
                      Profile = "Account.Profile"
                      Account = "id"
                when C
                  id = "id"
                then E
                  stream Account.Profile
                    streamId
                      Account = "id"
                      Profile = "Account.Profile"
              specification Appended
                when append E
                  for "id"
                  stream Account.Profile
                    streamId
                      Profile = "Account.Profile"
                      Account = "id"
                then E
                  no stream
        """;

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia, true)]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia, false)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, true)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, false)]
    void should_repair_route_tokens_without_changing_composite_part_lines(WorkspaceAuthoringFormatting formatting, bool renameSource)
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source));
        var workspace = ScreenplayWorkspace.Create("A", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => renameSource ? entry.Node is EventSourceSyntax : entry.Node is EventStreamSyntax);
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = renameSource ? "Account" : "Profile",
            NewName = renameSource ? "Customer" : "Details",
            Formatting = formatting
        });
        result.Conflicts.ShouldBeEmpty();
        result.Accepted.ShouldBeTrue();
        var nodes = WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node).ToArray();
        var reference = renameSource ? "Customer.Profile" : "Account.Details";
        var command = nodes.OfType<CommandStreamSyntax>().Single();
        $"{command.EventSource}.{command.Stream}".ShouldEqual(reference);
        var routes = nodes.OfType<SpecificationStreamSyntax>().ToArray();
        routes.Length.ShouldEqual(3);
        routes.All(route => $"{route.EventSource}.{route.Stream}" == reference).ShouldBeTrue();
        command.StreamIdParts.Count().ShouldEqual(2);
        routes.All(route => route.StreamIdParts.Count() == 2).ShouldBeTrue();
        nodes.OfType<EventSourceSyntax>().Single().Id.ShouldEqual("StoredAccount");
        nodes.OfType<EventStreamSyntax>().Single().Id.ShouldEqual("StoredProfile");
        var text = result.Workspace!.Documents.Single().Text;
        Assert.Equal(Encoding.UTF8.GetString(PartLines(Source)), Encoding.UTF8.GetString(PartLines(text)));
        if (formatting == WorkspaceAuthoringFormatting.PreserveTrivia)
        {
            var expected = Source.Replace("stream Account.Profile // route", $"stream {reference} // route", StringComparison.Ordinal)
                .Replace("stream Account.Profile\n", $"stream {reference}\n", StringComparison.Ordinal);
            expected = renameSource
                ? expected.Replace("eventsource Account\n", "eventsource Customer\n", StringComparison.Ordinal)
                : expected.Replace("  stream Profile\n", "  stream Details\n", StringComparison.Ordinal);
            text.ShouldEqual(expected);
        }
    }

    // Include exact indentation, headers, declared names/types, authored mapping order and literals.
    static byte[] PartLines(string source) => Encoding.UTF8.GetBytes(string.Join('\n', source.Split('\n').Where(line =>
        line.TrimStart().StartsWith("streamId", StringComparison.Ordinal) ||
        line.TrimStart().StartsWith("Account ", StringComparison.Ordinal) ||
        line.TrimStart().StartsWith("Profile ", StringComparison.Ordinal))));
}
