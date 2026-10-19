// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_inlining_a_standalone_event;

public class and_it_was_extracted_from_an_inline_event : given.a_standalone_event
{
    const string Original = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        name String\n        produces event Renamed // production intent\n          id \"OldName\" // identity intent\n          description \"A new name\" // description intent\n          documentation\n            ```markdown\n            Name history.\n            ```\n          tag audit\n          // mapping intent\n          name String = name // same comment\n";
    string _roundTrip;

    protected override string Source => Original;

    void Because()
    {
        var subject = WorkspaceSyntaxIndex.Create(_workspace).Entries.Single(entry => entry.Node is EventSyntax);
        var extracted = WorkspaceEventRefactorings.ProposeExtractInlineEvent(_workspace, subject.Handle, new WorkspaceAuthoringRequest
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring
        }).Workspace!;
        _roundTrip = Inline(Create(extracted.Documents[0].Text)).Workspace!.Documents[0].Text;
    }

    [Fact] void should_return_the_original_document_except_for_the_written_destination() => _roundTrip.Replace("          for projectId\n", string.Empty, StringComparison.Ordinal).ShouldEqual(Original);
}
