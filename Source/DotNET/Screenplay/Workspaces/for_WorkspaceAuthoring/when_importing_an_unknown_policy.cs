// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_importing_an_unknown_policy : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;
    WorkspaceDocument _document = null!;

    void Establish()
    {
        _document = Document("model", "model.play", "import External.Missing");
        Workspace = ScreenplayWorkspace.Create("App", [_document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
    }

    void Because() => _result = Workspace.ProposeAuthoring(Authoring() with
    {
        Documents = [new ReplaceWorkspaceSyntaxDocument(_document.Id, Syntax("import External.Missing\npersona User\n  policy Missing"))]
    });

    [Fact] void should_refuse_the_new_unresolved_policy() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_retain_the_compiler_unknown_policy_diagnostic() => _result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownPolicy).ShouldBeTrue();
    [Fact] void should_expose_no_accepted_candidate() => _result.Workspace.ShouldBeNull();
}
