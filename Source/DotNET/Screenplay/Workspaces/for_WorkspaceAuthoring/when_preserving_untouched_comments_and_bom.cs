// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_preserving_untouched_comments_and_bom : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        Concepts = WorkspaceDocument.Create(
            Concepts.Id,
            Concepts.StableKey,
            Concepts.Path,
            new byte[] { 0xef, 0xbb, 0xbf }.Concat(Encoding.UTF8.GetBytes($"// Keep exactly\r\n{ConceptsSource}")).ToArray());
        Registration = WorkspaceDocument.Create(
            Registration.Id,
            Registration.StableKey,
            Registration.Path,
            new byte[] { 0xef, 0xbb, 0xbf }.Concat(Encoding.UTF8.GetBytes($"// May be normalized\r\n{RegistrationSource}")).ToArray());
        Workspace = ScreenplayWorkspace.Create(Workspace.ApplicationName, [Concepts, Registration], Workspace.IdentityCatalog);
        RegistrationRoot = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Handle.Document == Registration.Id && entry.Parent is null);
    }

    void Because() => _result = Workspace.ProposeAuthoring(Authoring(new AddWorkspaceNode(
        RegistrationRoot.Handle, RegistrationRoot.Node, "imports", new ImportSyntax("External.Unused", SourceLocation.Start))));

    [Fact] void should_preserve_every_untouched_byte() => _result.Workspace.Documents.Single(document => document.Id == Concepts.Id).Bytes.AsSpan().SequenceEqual(Concepts.Bytes.AsSpan()).ShouldBeTrue();
    [Fact] void should_preserve_the_touched_document_bom() => _result.Workspace.Documents.Single(document => document.Id == Registration.Id).Encoding.ShouldEqual(WorkspaceTextEncoding.Utf8WithBom);
    [Fact] void should_disclose_comment_loss_only_for_the_touched_document() => _result.AuthoringDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).Location.Path.ShouldEqual(Registration.Path.Value);
}
