// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_refusing_an_unproven_stream_route_selection : given.a_reference_batch
{
    WorkspaceAuthoringRequest _request = null!;
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        Configure(("application.play", "import Account.Transactions\ntype Transactions\n  value String\neventsource Account\n  stream Transactions\n  stream Other\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        value String\n        stream Account.Transactions\n"));
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        var entry = index.Entries.Single(entry => entry.Node is CommandSyntax);
        var command = (CommandSyntax)entry.Node;
        var candidate = command.StreamCandidates.Single();
        var import = index.Entries.Single(entry => entry.Node is ImportSyntax);
        _request = new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft,
            Operations = [
                new ReplaceWorkspaceNode(entry.Handle, entry.Node, command with
                {
                    Stream = candidate with { PropertyCandidate = null, Stream = "Other" },
                    StreamCandidates = []
                }),
                new RemoveWorkspaceNode(import.Handle, import.Node)]
        };
    }

    void Because() => _result = Workspace.ProposeAuthoring(_request);

    [Fact] void should_refuse_the_unproven_selection() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_explain_the_failed_proof() => _result.Conflicts.Single().Message.ShouldContain("Cannot prove explicit stream candidate selection");
    [Fact] void should_name_the_candidate_document() => _result.Conflicts.Single().Document.ShouldEqual(Workspace.Documents.Single().Id);
    [Fact] void should_name_the_candidate_path() => _result.Conflicts.Single().Path.ShouldEqual(PortablePlayPath.Parse("application.play"));
    [Fact] void should_identify_the_candidate_occurrence() => _result.Conflicts.Single().Message.ShouldContain("disappeared: 'Account.Transactions'");
}
