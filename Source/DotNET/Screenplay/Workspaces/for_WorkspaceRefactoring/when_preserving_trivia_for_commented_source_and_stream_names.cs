// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_preserving_trivia_for_commented_source_and_stream_names : Specification
{
    const string Source = "// λογαριασμός\r\neventsource Account // Account ledger\r\n  identifier String\n  stream Profile // Profile history\r\n";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_leave_the_repeated_name_in_the_comment_untouched(bool renameSource)
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Bytes(Source));
        var workspace = ScreenplayWorkspace.Create("Accounts", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Accounts")));
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => renameSource ? entry.Node is EventSourceSyntax : entry.Node is EventStreamSyntax);
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = renameSource ? "Account" : "Profile",
            NewName = renameSource ? "Customer" : "Details",
            Formatting = WorkspaceAuthoringFormatting.PreserveTrivia
        });

        Assert.True(result.Accepted, string.Join('\n', result.Conflicts));
        var expected = renameSource
            ? Source.Replace("eventsource Account", "eventsource Customer", StringComparison.Ordinal)
            : Source.Replace("stream Profile", "stream Details", StringComparison.Ordinal);
        result.Workspace.Documents.Single().Bytes.SequenceEqual(Bytes(expected)).ShouldBeTrue();
    }

    static byte[] Bytes(string text) => [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(text)];
}
