// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_several_commands_produce_the_event : given.a_command_production
{
    const string Other = "\n      command Other\n        name ProjectName\n        produces ProjectRegistered\n          registeredAt = $context.occurred\n          name = name\n";

    [Fact]
    void should_admit_agreeing_producers_with_reordered_mappings()
    {
        Create(Source + Other);
        Repair = Find(DiagnosticCodes.UnknownEvent);
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request());
        Result.Accepted.ShouldBeTrue();
        Result.ExecutableReady.ShouldBeTrue();
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
        Create(Source.Replace("concept ProjectName : String", "concept ProjectName : String pii", StringComparison.Ordinal));
        Repair = Find(DiagnosticCodes.UnknownEvent);
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request());
        Result.Accepted.ShouldBeTrue();
        var syntax = WorkspaceSyntaxIndex.Create(Result.Workspace!);
        syntax.Entries.Select(entry => entry.Node).OfType<EventSyntax>().Single().Properties.First().Type.Name.ShouldEqual("ProjectName");
        syntax.Entries.Select(entry => entry.Node).OfType<ConceptSyntax>().Single().AttributeNames.ShouldContain("pii");
    }
}
