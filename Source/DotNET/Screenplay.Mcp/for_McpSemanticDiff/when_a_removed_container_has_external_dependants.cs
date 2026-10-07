// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_a_removed_container_has_external_dependants : given.a_semantic_comparison
{
    string _sliceId = string.Empty;

    void Because()
    {
        const string before = """
            module Projects
              feature Registration
                slice Automation Removed
                  event Recorded
                  reaction Internal
                    when Recorded
                slice Automation Other
                  reaction External
                    when Recorded
            """;
        const string after = "module Projects\n  feature Registration\n    slice Automation Other\n      reaction External\n        when Recorded\n";
        CompareSnapshots(before, after, catalog => SemanticIdentityCatalog.Create(
            catalog.Application,
            catalog.Documents,
            [.. catalog.Semantics.Where(assignment => !assignment.Address.Parts.Any(part => part.Kind == SemanticAddressPartKind.Slice && part.Key == "Removed"))],
            [.. catalog.EventContracts.Where(assignment => !assignment.Address.Parts.Any(part => part.Kind == SemanticAddressPartKind.Slice && part.Key == "Removed"))]));
        _sliceId = Workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice && assignment.Address.Name == "Removed").Id.ToString();
    }

    [Fact] void should_report_the_external_before_snapshot_dependant() => Items("dependants").Any(item => item.GetProperty("semanticId").GetString() == _sliceId && item.GetProperty("dependantAddress").GetString() == "Projects.Registration.Other.External" && item.GetProperty("snapshot").GetString() == "before").ShouldBeTrue();
    [Fact] void should_exclude_the_internal_reference_from_container_impact() => Items("dependants").Any(item => item.GetProperty("semanticId").GetString() == _sliceId && item.GetProperty("dependantAddress").GetString() == "Projects.Registration.Removed.Internal").ShouldBeFalse();
}
