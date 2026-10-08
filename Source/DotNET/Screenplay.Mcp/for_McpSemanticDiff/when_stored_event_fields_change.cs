// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_stored_event_fields_change : given.a_semantic_comparison
{
    void Because()
    {
        const string source = "module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        removed String\n        changed String\n";
        Workspace = Create(source);
        var retired = Workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Property && assignment.Address.Name == "removed").Address;
        var changed = source.Replace("        removed String\n", string.Empty, StringComparison.Ordinal).Replace("changed String", "changed Bool", StringComparison.Ordinal);
        var result = Workspace.Propose(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Operations = [new ReplaceWorkspaceDocument { Document = Workspace.Documents[0].Id, Bytes = [.. Encoding.UTF8.GetBytes(changed)] }],
            RetiredSemanticAddresses = [retired]
        });
        Assert.True(result.Success, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message).Concat(result.Diagnostics.Select(diagnostic => diagnostic.Message))));
        Proposal = new McpProposal(Workspace, result);
        Diff = Read();
    }

    [Fact] void should_report_the_removed_field() => Items("events").Any(item => item.GetProperty("changeKind").GetString() == "property-removed" && item.GetProperty("member").GetString() == "removed").ShouldBeTrue();
    [Fact] void should_report_the_changed_field_type() => Items("events").Any(item => item.GetProperty("changeKind").GetString() == "property-type-changed" && item.GetProperty("member").GetString() == "changed").ShouldBeTrue();
    [Fact] void should_flag_both_changes_as_contract_breaking() => Items("events").All(item => item.GetProperty("contractBreaking").GetBoolean()).ShouldBeTrue();
}
