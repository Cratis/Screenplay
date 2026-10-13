// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_preserving_a_source_witness : given.an_authoring_workspace
{
    const string Supplied = "// Keep aligned concepts\r\n\r\nconcept ProjectId   : Uuid\r\nconcept ProjectName : String\r\nconcept ProjectNotes : String";
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeAuthoring(Authoring() with
    {
        Formatting = WorkspaceAuthoringFormatting.PreserveExactSource,
        Documents = [new ReplaceWorkspaceSyntaxDocument(Concepts.Id, Syntax(Supplied)) { SourceWitness = Supplied }]
    });

    [Fact] void should_admit_the_structural_change() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_retain_the_supplied_bytes() => _result.Workspace!.Documents.Single(document => document.Id == Concepts.Id).Bytes.AsSpan().SequenceEqual(Encoding.UTF8.GetBytes(Supplied)).ShouldBeTrue();
    [Fact] void should_emit_no_normalization_warning() => _result.AuthoringDiagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("PLAY0288");
    [Fact] void should_keep_other_documents_exact() => _result.Workspace!.Documents.Single(document => document.Id == Registration.Id).Bytes.AsSpan().SequenceEqual(Registration.Bytes.AsSpan()).ShouldBeTrue();
}
