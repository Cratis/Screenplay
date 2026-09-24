// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_adding_a_different_module_description : given.a_valid_workspace
{
    WorkspaceTransactionResult _result = null!;

    void Because()
    {
        var described = WorkspaceDocument.Create(
            Registration.Id,
            Registration.StableKey,
            Registration.Path,
            Encoding.UTF8.GetBytes(RegistrationSource.Replace("module Projects\n", "module Projects\n  description \"First\"\n", StringComparison.Ordinal)));
        Workspace = ScreenplayWorkspace.Create("Projects", [described, Concepts], Workspace.IdentityCatalog);
        _result = Workspace.Propose(Request(new AddWorkspaceDocument
        {
            StableKey = "second-description",
            Path = PortablePlayPath.Parse("ZZZ.play"),
            Bytes = Bytes("module Projects\n  description \"Second\"")
        }));
    }

    [Fact] void should_accept_the_transaction() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_report_an_owner_conflict() => _result.Conflicts.Any(conflict => conflict.Kind == WorkspaceConflictKind.ConflictingOwner).ShouldBeFalse();
    [Fact] void should_keep_the_warning() => _result.Workspace!.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ConflictingDescriptionAcrossFiles && diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeTrue();
}
