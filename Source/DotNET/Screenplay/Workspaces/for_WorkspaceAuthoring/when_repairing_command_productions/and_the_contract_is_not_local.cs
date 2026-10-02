// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_the_contract_is_not_local : given.a_command_production
{
    [Fact]
    void should_resolve_an_imported_event_without_a_missing_event_diagnostic()
    {
        Create("import External.ProjectRegistered\n" + Source);
        Workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEvent).ShouldBeFalse();
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
    }

    [Fact]
    void should_resolve_a_cross_file_declaration_without_a_missing_event_diagnostic()
    {
        Create(Source, "module Projects\n  feature Other\n    slice StateChange Other\n      event ProjectRegistered\n        name ProjectName\n        registeredAt DateTime\n");
        Workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEvent).ShouldBeFalse();
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
    }

    [Fact]
    void should_not_infer_for_an_agreeing_producer_in_another_file()
    {
        Create(Source, "module Projects\n  feature Other\n    slice StateChange Other\n      command Other\n        name ProjectName\n        produces ProjectRegistered\n          name = name\n          registeredAt = $context.occurred\n");
        AssertMissingEventWithoutRepair();
    }

    [Fact]
    void should_not_infer_for_an_agreeing_reaction_alongside_a_command()
    {
        Create(Source + "\n      reaction React\n        on Unknown\n        produces ProjectRegistered\n          name = name\n          registeredAt = $context.occurred\n");
        AssertMissingEventWithoutRepair();
    }

    [Fact]
    void should_not_infer_when_another_document_has_parser_errors()
    {
        Create(Source, "notScreenplay\n");
        WorkspaceSyntaxIndex.Create(Workspace).Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
        AssertMissingEventWithoutRepair();
    }

    [Fact]
    void should_not_infer_when_a_partial_document_hides_an_agreeing_producer()
    {
        Create(Source, "module Projects\n  feature Other\n    slice StateChange Other\n      command Other\n        name ProjectName\n        produces ProjectRegistered\n          name = name\n          registeredAt = $context.occurred\n        invalid\n");
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        index.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
        index.Entries.Any(entry => entry.Handle.Document == Workspace.Documents.Single(document => document.Path.Value == "document-1.play").Id).ShouldBeFalse();
        AssertMissingEventWithoutRepair();
    }

    void AssertMissingEventWithoutRepair()
    {
        Workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEvent && diagnostic.Location.Path == "document-0.play").ShouldBeTrue();
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
    }
}
