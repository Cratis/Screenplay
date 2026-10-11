// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_numbering_enum_members : Specification
{
    const string Source = "concept Status : Enum // stored identity\n  draft // first member\n  @validate // escaped member\nmodule M\n  feature F\n    slice StateChange S\n      event E\n        status Status\n";
    ScreenplayWorkspace _workspace;
    WorkspaceNodeHandle _subject;
    WorkspaceAuthoringResult _result;

    void Establish()
    {
        _workspace = Create(Source);
        _subject = WorkspaceSyntaxIndex.Create(_workspace).Entries.Single(entry => entry.Node is ConceptSyntax).Handle;
    }

    void Because() => _result = WorkspaceEnumerationRefactorings.ProposeNumberMembers(_workspace, _subject, Request(_workspace));

    [Fact] void should_accept() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_pin_current_positions() => _result.Workspace!.Documents[0].Text.ShouldContain("draft = 0");
    [Fact] void should_keep_escaped_names() => _result.Workspace!.Documents[0].Text.ShouldContain("@validate = 1");
    [Fact] void should_preserve_comments() => _result.Workspace!.Documents[0].Text.ShouldContain("// first member");
    [Fact] void should_preserve_executable_bytes() => WorkspaceRepairVerification.SameModel(_workspace, _result.Workspace!).ShouldBeTrue();
    [Fact] void should_preserve_catalog() => WorkspaceRepairVerification.KeepsCatalog(_workspace, _result.Workspace!).ShouldBeTrue();
    [Fact] void should_not_modify_original_source() => _workspace.Documents[0].Text.ShouldEqual(Source);
    [Fact] void should_require_formatting_consent() => WorkspaceEnumerationRefactorings.ProposeNumberMembers(_workspace, _subject, Request(_workspace) with { Formatting = WorkspaceAuthoringFormatting.PreserveTrivia }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.FormattingConsentRequired);
    [Fact] void should_refuse_stale_revision() => WorkspaceEnumerationRefactorings.ProposeNumberMembers(_workspace, _subject, Request(_workspace) with { ExpectedRevision = WorkspaceRevision.Parse("wsrev1:" + new string('0', 64)) }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);

    [Theory]
    [InlineData("concept Status : Enum\n  draft = 0\n  issued = 1")]
    [InlineData("concept Status : Enum\n  draft = 0\n  issued")]
    [InlineData("concept Status : String")]
    void should_never_renumber_or_number_non_enums(string source)
    {
        var workspace = Create(source);

        // Errorful documents expose no editable concept handle; a previous handle cannot make them editable.
        var subject = WorkspaceSyntaxIndex.Create(workspace).Entries.SingleOrDefault(entry => entry.Node is ConceptSyntax)?.Handle ?? _subject;
        WorkspaceEnumerationRefactorings.ProposeNumberMembers(workspace, subject, Request(workspace)).Accepted.ShouldBeFalse();
    }

    static ScreenplayWorkspace Create(string source) => ScreenplayWorkspace.Create("Enums", [WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Enums")));

    static WorkspaceAuthoringRequest Request(ScreenplayWorkspace workspace) => new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Validation = WorkspaceAuthoringValidation.Authoring
    };
}
