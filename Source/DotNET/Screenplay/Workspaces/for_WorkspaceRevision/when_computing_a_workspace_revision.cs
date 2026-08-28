// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRevision;

public class when_computing_a_workspace_revision : Specification
{
    WorkspaceRevision _first;
    WorkspaceRevision _same;
    WorkspaceRevision _different;
    WorkspaceRevision _parsed;

    void Because()
    {
        _first = WorkspaceRevision.Compute(Encoding.UTF8.GetBytes("workspace-a"));
        _same = WorkspaceRevision.Compute(Encoding.UTF8.GetBytes("workspace-a"));
        _different = WorkspaceRevision.Compute(Encoding.UTF8.GetBytes("workspace-b"));
        _parsed = WorkspaceRevision.Parse(_first.ToString());
    }

    [Fact] void should_be_deterministic() => _same.ShouldEqual(_first);
    [Fact] void should_change_with_the_exact_workspace_projection() => _different.ShouldNotEqual(_first);
    [Fact] void should_match_the_canonical_golden_revision() => _first.ToString().ShouldEqual("wsrev1:1396c3d74f0b79ec299438edfaa6814d2a8fdd8a4085aaa6523200fbc3dfc759");
    [Fact] void should_parse_the_canonical_value() => _parsed.ShouldEqual(_first);
    [Fact] void should_reject_a_semantic_revision_domain() => WorkspaceRevision.TryParse(_first.ToString().Replace("wsrev1:", "rev1:", StringComparison.Ordinal), out _).ShouldBeFalse();
    [Fact] void should_reject_null() => WorkspaceRevision.TryParse(null, out _).ShouldBeFalse();
    [Fact] void should_reject_uppercase_hash_text() => WorkspaceRevision.TryParse(UppercaseHash(_first), out _).ShouldBeFalse();
    [Fact] void should_reject_a_short_hash() => WorkspaceRevision.TryParse("wsrev1:abc", out _).ShouldBeFalse();
    [Fact] void should_leave_the_default_revision_unset() => default(WorkspaceRevision).IsSet.ShouldBeFalse();

    static string UppercaseHash(WorkspaceRevision revision)
    {
        const string prefix = "wsrev1:";
        return $"{prefix}{revision.ToString()[prefix.Length..].ToUpperInvariant()}";
    }
}
