// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.for_BaselineCommand;

public class when_admitting_and_checking : given.a_release_workspace
{
    [Fact] void should_pass_with_no_baseline() => Run("baseline", "check", Root).ShouldEqual(0);

    [Fact]
    void should_preserve_all_model_and_identity_revisions()
    {
        var state = File.ReadAllBytes(IdentityPath);
        var source = File.ReadAllBytes(Path.Combine(Root, "application.play"));
        var bytes = SemanticModelSerializer.Serialize(Workspace.Compilation.Value!.Model);
        Admit();
        var reopened = McpState.Deserialize(File.ReadAllBytes(IdentityPath)).Open(new(Root));
        reopened.Revision.ShouldEqual(Workspace.Revision);
        reopened.IdentityCatalog.Revision.ShouldEqual(Workspace.IdentityCatalog.Revision);
        reopened.Compilation.Value!.Model.Revision.ShouldEqual(Workspace.Compilation.Value.Model.Revision);
        SemanticModelSerializer.Serialize(reopened.Compilation.Value.Model).ShouldEqual(bytes);
        File.ReadAllBytes(IdentityPath).ShouldEqual(state);
        File.ReadAllBytes(Path.Combine(Root, "application.play")).ShouldEqual(source);
        Run("baseline", "check", Root).ShouldEqual(0);
        File.Exists(Path.Combine(Root, ".screenplay", "pending.json")).ShouldBeFalse();
    }

    [Fact]
    void should_fail_changed_shapes_and_readmission_with_exit_one()
    {
        Admit();
        var before = File.ReadAllBytes(BaselinePath);
        File.WriteAllText(Path.Combine(Root, "application.play"), Source.Replace("value String", "value String?", StringComparison.Ordinal));
        Run("baseline", "check", Root).ShouldEqual(1);
        Output.ToString().ShouldContain("BaselinedRevisionChanged");
        Output.ToString().ShouldContain("optional");
        Run("baseline", "admit", Root, "--release", "v2").ShouldEqual(1);
        File.ReadAllBytes(BaselinePath).ShouldEqual(before);
    }

    [Fact]
    void should_report_missing_revisions_with_exit_one()
    {
        Admit();
        var baseline = ReleaseBaselineStore.ReadBytes(File.ReadAllBytes(BaselinePath), Workspace.IdentityCatalog.Application).Document!;
        var removed = baseline.Entries[0] with { Event = Semantics.EventContractId.Create(Workspace.IdentityCatalog.Application, "removed").ToString() };
        File.WriteAllBytes(BaselinePath, ReleaseBaselineStore.Serialize(baseline with { Entries = baseline.Entries.Add(removed) }));
        Run("baseline", "check", Root).ShouldEqual(1);
        Output.ToString().ShouldContain("BaselinedRevisionMissing");
    }

    [Fact]
    void should_fail_closed_for_unreadable_baselines()
    {
        File.WriteAllText(BaselinePath, "<<<<<<< main");
        Run("baseline", "check", Root).ShouldEqual(2);
        Run("baseline", "admit", Root, "--release", "v1").ShouldEqual(2);
        File.ReadAllText(BaselinePath).ShouldEqual("<<<<<<< main");
    }

    [Fact]
    void should_refuse_absent_identities_without_creating_them()
    {
        File.Delete(IdentityPath);
        Run("baseline", "admit", Root, "--release", "v1").ShouldEqual(2);
        Error.ToString().ShouldContain("PersistedIdentitiesRequired");
        File.Exists(BaselinePath).ShouldBeFalse();
        File.Exists(IdentityPath).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_pending_recovery_without_writes()
    {
        var marker = Path.Combine(Root, ".screenplay", "pending.json");
        File.WriteAllText(marker, "pending");
        Run("baseline", "admit", Root, "--release", "v1").ShouldEqual(2);
        Error.ToString().ShouldContain("PendingOperation");
        File.Exists(BaselinePath).ShouldBeFalse();
        File.ReadAllText(marker).ShouldEqual("pending");
    }

    [Fact]
    void should_refuse_nonbinding_source()
    {
        File.WriteAllText(Path.Combine(Root, "application.play"), Source.Replace("value String", "value Unknown", StringComparison.Ordinal));
        Run("baseline", "admit", Root, "--release", "v1").ShouldEqual(2);
        File.Exists(BaselinePath).ShouldBeFalse();
    }

    [Fact]
    void should_admit_the_shipped_shape_instead_of_later_source()
    {
        var path = Path.Combine(Root, "shipped.json");
        File.WriteAllBytes(path, SemanticModelSerializer.Serialize(Workspace.Compilation.Value!.Model));
        File.WriteAllText(Path.Combine(Root, "application.play"), Source.Replace("value String", "value String?", StringComparison.Ordinal));
        Run("baseline", "admit", Root, "--release", "shipped", "--from-esm", path).ShouldEqual(0);
        Run("baseline", "check", Root).ShouldEqual(1);
    }

    [Fact]
    void should_refuse_noncanonical_shipped_bytes()
    {
        var path = Path.Combine(Root, "shipped.json");
        File.WriteAllText(path, "{}");
        Run("baseline", "admit", Root, "--release", "v1", "--from-esm", path).ShouldEqual(2);
        File.Exists(BaselinePath).ShouldBeFalse();
    }

    [Theory]
    [InlineData("baseline")]
    [InlineData("baseline", "other")]
    [InlineData("baseline", "admit", "root")]
    [InlineData("baseline", "check", "root", "--release", "v1")]
    void should_reject_invalid_arguments(params string[] args) => Run(args).ShouldEqual(2);
}
