// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRepairVerification;

public class when_replacing_a_studio_text_rule : Specification
{
    ScreenplayWorkspace _workspace;
    WorkspaceAuthoringResult _result;

    void Establish()
    {
        const string Source = "concept Ref : String\n  validate\n    matches \"^(?![\\s\\S]*B)\" severity warning message \"No B\"\n";
        var document = WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(Source));
        _workspace = ScreenplayWorkspace.Create("TextRules", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("TextRules")));
    }

    void Because()
    {
        var index = WorkspaceSyntaxIndex.Create(_workspace);
        var diagnostic = index.RepairableDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.DeprecatedStudioTextRulePattern);
        var repair = WorkspaceDiagnosticRepairs.Find(index, _workspace.Revision, diagnostic).Single();
        _result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, repair, new()
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring
        });
    }

    [Fact] void should_accept_the_repair() => Assert.True(_result.Accepted, string.Join("; ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_keep_the_executable_revision() => _result.Workspace!.Compilation.Value!.Model.Revision.ShouldEqual(_workspace.Compilation.Value!.Model.Revision);
    [Fact] void should_replace_only_the_rule_shape() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<ValidationRuleSyntax>().Single().Rule.ShouldEqual(ValidationRuleKind.NotContains);
    [Fact] void should_remove_the_warning() => _result.Workspace!.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.DeprecatedStudioTextRulePattern).ShouldBeFalse();
}
