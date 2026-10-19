// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_a_plain_production_copies_the_identifier : given.a_command_production
{
    const string StandaloneSource = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        produces Renamed\n          for projectId\n          projectId = projectId\n          name = \"something\"\n      event Renamed\n        projectId Uuid\n        name String\n";

    int _transactions;
    string _source = null!;
    bool _hasRepair;

    void Establish()
    {
        Create(StandaloneSource);
        _transactions = WorkspaceRepairVerification.TransactionCount(Workspace);
        _source = Workspace.Documents.Single().Text;
    }

    void Because() => _hasRepair = HasRepair(DiagnosticCodes.EventSourceIdInPayload);

    [Fact] void should_report_the_copy_as_information() => Workspace.Compilation.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.EventSourceIdInPayload).Severity.ShouldEqual(DiagnosticSeverity.Information);
    [Fact] void should_offer_no_repair() => _hasRepair.ShouldBeFalse();
    [Fact] void should_not_run_a_transaction() => WorkspaceRepairVerification.TransactionCount(Workspace).ShouldEqual(_transactions);
    [Fact] void should_not_change_the_source() => Workspace.Documents.Single().Text.ShouldEqual(_source);
}
