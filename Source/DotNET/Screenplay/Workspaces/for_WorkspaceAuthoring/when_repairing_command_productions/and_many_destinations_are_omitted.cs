// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_many_destinations_are_omitted : given.a_command_production
{
    const int ProductionCount = 8;

    void Establish()
    {
        Create(DestinationSource + string.Concat(Enumerable.Range(1, ProductionCount - 1).Select(number =>
            $"\n      command Register{number}\n        projectId Uuid identifier\n        produces Registered{number}\n      event Registered{number}\n")));
        Workspace.Compilation.Success.ShouldBeTrue();
        Workspace.Compilation.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination).ShouldEqual(ProductionCount);
    }

    [Fact]
    void should_verify_only_the_requested_subject_when_proposing_by_code_and_handle()
    {
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        var subject = index.Entries.Single(entry => entry.Node is ProducesSyntax production && production.Event == "Registered7");
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, DiagnosticCodes.OmittedProductionDestination, subject.Handle, Request());
        Result.Accepted.ShouldBeTrue();
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(1);
    }

    [Fact]
    void should_run_one_proposal_transaction_after_discovery_without_verifying_other_diagnostics()
    {
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        Repair = WorkspaceDiagnosticRepairs.Find(index, Workspace.Revision, index.RepairableDiagnostics.Last(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination)).Single();
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(1);
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request());
        Result.Accepted.ShouldBeTrue();
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(2);
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request());
        Result.Accepted.ShouldBeTrue();
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(3);
    }

    [Fact]
    void should_cache_only_verdict_fields_not_candidate_workspaces_or_write_plans()
    {
        Discover(Workspace).Length.ShouldEqual(ProductionCount);
        var verification = typeof(WorkspaceProductionRepairs).GetNestedType("Verification", BindingFlags.NonPublic)!;
        var subjects = verification.GetProperty("Subjects", BindingFlags.Instance | BindingFlags.NonPublic)!.PropertyType;
        var cached = subjects.GetGenericArguments()[1];
        cached.GetGenericTypeDefinition().ShouldEqual(typeof(Lazy<>));
        var fields = cached.GetGenericArguments()[0].GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        fields.Length.ShouldEqual(2);
        fields.Select(field => field.FieldType).ShouldContainOnly(typeof(bool), typeof(ImmutableArray<WorkspaceConflict>));
    }

    [Fact]
    void should_not_retain_candidate_diagnostics_for_multiple_refused_version_upgrades()
    {
        Create(DestinationSource.Replace(VersionAnchor, string.Empty, StringComparison.Ordinal)
            .Replace("          registeredAt = $context.occurred\n", string.Empty, StringComparison.Ordinal)
            .Replace("        registeredAt DateTime\n", string.Empty, StringComparison.Ordinal) +
            string.Concat(Enumerable.Range(1, ProductionCount - 1).Select(number =>
                $"\n      command Register{number}\n        projectId Uuid identifier\n        produces Registered{number}\n      event Registered{number}\n")));
        Workspace.Compilation.Success.ShouldBeTrue();
        Workspace.Compilation.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        var first = index.Entries.First(entry => entry.Node is ProducesSyntax);
        var production = (ProducesSyntax)first.Node;
        var preview = Workspace.ProposeAuthoring(Request() with
        {
            Operations = [new ReplaceWorkspaceNode(first.Handle, production, production with { For = new PathExpressionSyntax("projectId", production.Location) })]
        });
        preview.Accepted.ShouldBeTrue();
        preview.Workspace!.Compilation.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V2);
        preview.AuthoringDiagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination).ShouldEqual(ProductionCount - 1);
        preview.ExecutableDiagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination).ShouldEqual(ProductionCount - 1);
        Discover(Workspace).ShouldBeEmpty();
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(ProductionCount);
        var subjects = CachedSubjects();
        subjects.Count.ShouldEqual(ProductionCount);
        foreach (var cached in subjects.Values)
        {
            var verdict = cached!.GetType().GetProperty("Value")!.GetValue(cached)!;
            var fields = verdict.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fields.Length.ShouldEqual(2);
            fields.Select(field => field.FieldType).ShouldContainOnly(typeof(bool), typeof(ImmutableArray<WorkspaceConflict>));
        }

        foreach (var subject in index.Entries.Where(entry => entry.Node is ProducesSyntax))
        {
            Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, DiagnosticCodes.OmittedProductionDestination, subject.Handle, Request());
            Result.Accepted.ShouldBeFalse();
            Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidOperation);
            Result.AuthoringDiagnostics.ShouldBeEmpty();
            Result.ExecutableDiagnostics.ShouldBeEmpty();
        }

        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(ProductionCount);
    }

    [Fact]
    void should_run_only_its_own_transaction_when_discovery_has_published_an_uncompleted_verdict()
    {
        Repair = Discover(Workspace)[^1];
        var subjects = CachedSubjects();
        var key = (Repair.Subject, Repair.DiagnosticCode, WorkspaceAuthoringValidation.Authoring, Request().ReferencePolicy);
        var completed = subjects[key]!;
        var verdict = completed.GetType().GetProperty("Value")!.GetValue(completed)!;
        var evaluated = false;

        // Replace the completed lazy with a published but unstarted discovery lazy, without timing hooks.
        var pending = typeof(and_many_destinations_are_omitted).GetMethod(nameof(Uncompleted), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(verdict.GetType()).Invoke(null, [verdict, (Action)(() => evaluated = true)])!;
        subjects[key] = pending;
        var before = WorkspaceProductionRepairs.TransactionCount(Workspace);
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request());
        Result.Accepted.ShouldBeTrue();
        evaluated.ShouldBeFalse();
        ((bool)pending.GetType().GetProperty("IsValueCreated")!.GetValue(pending)!).ShouldBeFalse();
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(before + 1);
    }

    [Fact]
    void should_cache_verified_discovery_across_indexes_and_pages_of_the_same_snapshot()
    {
        Discover(Workspace).Length.ShouldEqual(ProductionCount);
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(ProductionCount);
        Discover(Workspace).Length.ShouldEqual(ProductionCount);
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(ProductionCount);
    }

    [Fact]
    void should_verify_again_for_a_new_revision_and_never_return_stale_subjects()
    {
        Repair = Discover(Workspace)[^1];
        Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request());
        Result.Accepted.ShouldBeTrue();
        var candidate = Result.Workspace!;
        candidate.Revision.ShouldNotEqual(Workspace.Revision);
        var repairs = Discover(candidate);
        repairs.Length.ShouldEqual(ProductionCount - 1);
        repairs.All(repair => repair.Subject.Revision == candidate.Revision).ShouldBeTrue();
        repairs.Any(repair => repair.Subject == Repair.Subject).ShouldBeFalse();
        WorkspaceProductionRepairs.TransactionCount(candidate).ShouldEqual(ProductionCount - 1);
    }

    IDictionary CachedSubjects()
    {
        var verification = typeof(WorkspaceProductionRepairs).GetField("_verification", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        object?[] arguments = [Workspace, null];
        ((bool)verification.GetType().GetMethod("TryGetValue")!.Invoke(verification, arguments)!).ShouldBeTrue();
        var snapshot = arguments[1]!;
        return (IDictionary)snapshot.GetType().GetProperty("Subjects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(snapshot)!;
    }

    static Lazy<T> Uncompleted<T>(object verdict, Action evaluated) => new(() =>
    {
        evaluated();
        return (T)verdict;
    });

    static WorkspaceDiagnosticRepair[] Discover(ScreenplayWorkspace workspace)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        return [.. index.RepairableDiagnostics.SelectMany(diagnostic => WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic))];
    }
}
