// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_ReleaseBaseline;

public class when_checking_proposal_contracts : given.a_baseline_workspace
{
    [Theory]
    [InlineData("concept", "String pii", "Int pii")]
    [InlineData("enum", "sent = 4", "sent = 4\n  archived = 8")]
    [InlineData("type", "value Protected", "renamed Protected")]
    void should_list_every_affected_frozen_revision(string change, string before, string after)
    {
        var candidate = change == "type" ? Renamed("value", "renamed") : Changed(Source.Replace(before, after, StringComparison.Ordinal));
        Assert.True(candidate.Compilation.Success, string.Join('\n', candidate.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var check = WorkspaceReleaseBaseline.Check(Workspace, candidate, Read(Baseline));
        check.Success.ShouldBeFalse();
        check.Violations.Length.ShouldEqual(2);
        check.Violations.Select(value => value.Kind).Distinct().Single().ShouldEqual("BaselinedRevisionChanged");
        check.Violations.All(value => value.Path is not null).ShouldBeTrue();
    }

    [Fact]
    void should_report_a_removed_historical_generation()
    {
        var candidate = Changed(Source.Replace("event Recorded generation 1\n        details Details[]\n      ", string.Empty, StringComparison.Ordinal));
        // Removing revision 1 leaves a generation gap, which is also refused by executable binding.
        candidate.Compilation.Success.ShouldBeFalse();
        var check = WorkspaceReleaseBaseline.Check(Workspace, candidate, Read(Baseline));
        check.Violations.Single().Kind.ShouldEqual("BaselinedRevisionMissing");
        check.Violations.Single().Revision.ShouldEqual(1U);
        ReleaseBaseline.Check(candidate, Read(Baseline)).Violations.Single().ShouldEqual(check.Violations.Single());
        ReleaseBaseline.Admit(candidate, Read(Baseline), "v2", At).Failure.ShouldEqual("ModelDoesNotBind");
    }

    [Theory]
    [InlineData("description")]
    [InlineData("concept-name")]
    void should_allow_excluded_changes_even_with_an_unreadable_baseline(string change)
    {
        var candidate = change == "description" ? Changed(Source.Replace("concept Protected : String pii", "concept Protected : String pii\n  description \"documented\"", StringComparison.Ordinal)) : Renamed("Protected", "ProtectedValue");
        WorkspaceReleaseBaseline.Check(Workspace, candidate, Read(Baseline)).Success.ShouldBeTrue();
        WorkspaceReleaseBaseline.Check(Workspace, candidate, new("unreadable", null, null, "conflict")).Success.ShouldBeTrue();
    }

    ScreenplayWorkspace Renamed(string before, string after)
    {
        var entry = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is ConceptSyntax { Name: "Protected" } && before == "Protected" || entry.Node is PropertySyntax { Name: "value" } && before == "value");
        var proposal = Workspace.ProposeRename(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Target = entry.Handle,
            ExpectedName = before,
            NewName = after,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
        });
        Assert.True(proposal.Accepted, string.Join('\n', proposal.Conflicts.Select(conflict => conflict.Message)));
        return proposal.Workspace!;
    }

    [Fact]
    void should_fail_closed_for_an_unreadable_baseline_when_contracts_change()
    {
        var check = WorkspaceReleaseBaseline.Check(Workspace, Changed(Source.Replace("note String?", "note Int?", StringComparison.Ordinal)), new("unreadable", null, null, "conflict"));
        check.Failure.ShouldEqual("UnreadableReleaseBaseline");
    }
}
