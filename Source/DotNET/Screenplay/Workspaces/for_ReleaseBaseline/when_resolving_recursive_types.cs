// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ReleaseBaseline;

public class when_resolving_recursive_types : Specification
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    void should_admit_roundtrip_and_check_self_and_mutually_recursive_types(bool mutual, int distance)
    {
        var source = "type Node\n  value String\n  next " + (mutual ? "Link?\ntype Link\n  nodes Node[]\n" : "Node?\n") + "module M\n  feature F\n    slice StateChange Record\n      event Recorded\n        root Node\n";
        var workspace = Workspace(source);
        Assert.True(workspace.Compilation.Success, string.Join('\n', workspace.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var admitted = ReleaseBaseline.Admit(workspace, ReleaseBaselineStore.ReadBytes(null, workspace.IdentityCatalog.Application), "v1", DateTimeOffset.UnixEpoch);
        admitted.Success.ShouldBeTrue();
        var baseline = ReleaseBaselineStore.ReadBytes(ReleaseBaselineStore.Serialize(admitted.Document!), workspace.IdentityCatalog.Application);
        baseline.Status.ShouldEqual("valid");
        var schema = baseline.Document!.Entries.Single().Schema;
        Encoding.UTF8.GetString(ReleaseSchemaCanonical.Serialize(schema)).ShouldContain("\"kind\":\"ref\",\"ref\":" + distance);
        ReleaseBaseline.Check(workspace, baseline).Success.ShouldBeTrue();
        var renamed = Workspace(source.Replace("Node", "Renamed", StringComparison.Ordinal).Replace("Link", "Other", StringComparison.Ordinal));
        ReleaseSchemaCanonical.Hash(ReleaseBaseline.Admit(renamed, ReleaseBaselineStore.ReadBytes(null, renamed.IdentityCatalog.Application), "v1", DateTimeOffset.UnixEpoch).Document!.Entries.Single().Schema).ShouldEqual(ReleaseSchemaCanonical.Hash(schema));
        var changed = Workspace(source.Replace("value String", "value Int", StringComparison.Ordinal), workspace.IdentityCatalog);
        ReleaseBaseline.Check(changed, baseline).Violations.Single().Kind.ShouldEqual("BaselinedRevisionChanged");
        WorkspaceReleaseBaseline.Check(workspace, changed, baseline).Violations.Single().Kind.ShouldEqual("BaselinedRevisionChanged");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    void should_refuse_a_recursive_reference_outside_its_composite_path(int distance)
    {
        var schema = new ReleaseSchema("Recorded", [new("root", false, false, new("ref", Ref: distance))]);
        Assert.Throws<InvalidReleaseBaseline>(() => ReleaseSchemaCanonical.Serialize(schema));
    }

    static ScreenplayWorkspace Workspace(string source, SemanticIdentityCatalog? catalog = null)
    {
        catalog ??= SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Recursive"));
        var document = WorkspaceDocument.Create(catalog.ResolveDocument("application"), "application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(source));
        return ScreenplayWorkspace.Create("Recursive", [document], catalog);
    }
}
