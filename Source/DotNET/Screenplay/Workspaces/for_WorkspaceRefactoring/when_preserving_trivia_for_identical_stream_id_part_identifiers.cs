// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_preserving_trivia_for_identical_stream_id_part_identifiers : Specification
{
    const string Source = "// Περίοδος\r\nconcept Period : String // Period\r\neventsource Account\r\n  stream Profile\n    streamId\r\n      Period\tPeriod // Period stays here\r\n      key String";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_patch_only_the_identifier_at_its_syntactic_position(bool renameType)
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Bytes(Source));
        var workspace = ScreenplayWorkspace.Create("Accounts", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Accounts")));
        var entries = WorkspaceSyntaxIndex.Create(workspace).Entries;
        WorkspaceAuthoringResult result;
        if (renameType)
        {
            result = workspace.ProposeRename(new()
            {
                ExpectedRevision = workspace.Revision,
                ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
                Target = entries.Single(entry => entry.Node is ConceptSyntax).Handle,
                ExpectedName = "Period",
                NewName = "Month",
                Formatting = WorkspaceAuthoringFormatting.PreserveTrivia
            });
        }
        else
        {
            var part = entries.First(entry => entry.Node is EventStreamIdPartSyntax);
            result = workspace.ProposeAuthoring(new()
            {
                ExpectedRevision = workspace.Revision,
                ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
                Validation = WorkspaceAuthoringValidation.Authoring,
                Formatting = WorkspaceAuthoringFormatting.PreserveTrivia,
                Operations = [new ReplaceWorkspaceNode(part.Handle, part.Node, (EventStreamIdPartSyntax)part.Node with { Name = "Month" })]
            });
        }

        Assert.True(result.Accepted, string.Join('\n', result.Conflicts));
        var expected = renameType
            ? Source.Replace("concept Period", "concept Month", StringComparison.Ordinal).Replace("Period\tPeriod", "Period\tMonth", StringComparison.Ordinal)
            : Source.Replace("Period\tPeriod", "Month\tPeriod", StringComparison.Ordinal);
        result.Workspace.Documents.Single().Bytes.SequenceEqual(Bytes(expected)).ShouldBeTrue();
    }

    static byte[] Bytes(string text) => [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(text)];
}
