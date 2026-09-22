// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_preserving_exact_trivia : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;
    byte[] _expected = [];

    void Establish()
    {
        const string source = "// Ελληνικά ProjectName\r\nconcept ProjectName : String // unchanged ProjectName\n\r\nmodule Projects\r\n  feature Registration\n    slice StateChange Register\r\n      command Register\r\n        name ProjectName // ProjectName";
        Concepts = WorkspaceDocument.Create("unicode", PortablePlayPath.Parse("unicode.play"), [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(source)]);
        Workspace = ScreenplayWorkspace.Create("Projects", [Concepts], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        _expected = [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(source.Replace("concept ProjectName", "concept ProjectTitle", StringComparison.Ordinal).Replace("name ProjectName", "name ProjectTitle", StringComparison.Ordinal))];
    }

    void Because() => _result = Workspace.ProposeRename(Rename<ConceptSyntax>("ProjectName", "ProjectTitle"));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_preserve_every_other_byte() => _result.Workspace.Documents.Single().Bytes.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_not_disclose_canonicalization() => _result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Message.Contains("canonically", StringComparison.Ordinal)).ShouldBeFalse();
}
