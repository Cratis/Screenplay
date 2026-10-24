// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_several_commands_produce_the_event : given.a_command_production
{
    const string Other = "\n      command Other\n        name ProjectName\n        produces ProjectRegistered\n          registeredAt = $context.occurred\n          name = name\n";

    [Fact]
    void should_admit_agreeing_producers_with_reordered_mappings()
    {
        var workspace = CreateWorkspace(Source + Other);
        var repair = Find(workspace, DiagnosticCodes.UnknownEvent);
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, Request(workspace));
        result.Accepted.ShouldBeTrue();
        result.ExecutableReady.ShouldBeTrue();
    }

    [Fact]
    void should_refuse_conflicting_types()
    {
        Create(Source + Other.Replace("name ProjectName", "name String", StringComparison.Ordinal));
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_incomplete_shapes()
    {
        Create(Source + Other.Replace("          name = name\n", string.Empty, StringComparison.Ordinal));
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
    }

    [Fact]
    void should_preserve_concept_compliance_markings()
    {
        var workspace = CreateWorkspace(Source.Replace("concept ProjectName : String", "concept ProjectName : String pii", StringComparison.Ordinal));
        var repair = Find(workspace, DiagnosticCodes.UnknownEvent);
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, Request(workspace));
        result.Accepted.ShouldBeTrue();
        var syntax = WorkspaceSyntaxIndex.Create(result.Workspace!);
        syntax.Entries.Select(entry => entry.Node).OfType<EventSyntax>().Single().Properties.First().Type.Name.ShouldEqual("ProjectName");
        syntax.Entries.Select(entry => entry.Node).OfType<ConceptSyntax>().Single().AttributeNames.ShouldContain("pii");
    }

    static ScreenplayWorkspace CreateWorkspace(string source) => ScreenplayWorkspace.Create(
        "Projects",
        [WorkspaceDocument.Create("document-0", PortablePlayPath.Parse("document-0.play"), Encoding.UTF8.GetBytes(source))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    static WorkspaceAuthoringRequest Request(ScreenplayWorkspace workspace) => new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };

    static WorkspaceDiagnosticRepair Find(ScreenplayWorkspace workspace, string code) => WorkspaceSyntaxIndex.Create(workspace).RepairableDiagnostics
        .Where(diagnostic => diagnostic.Code == code)
        .SelectMany(diagnostic => WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic)).First();
}
