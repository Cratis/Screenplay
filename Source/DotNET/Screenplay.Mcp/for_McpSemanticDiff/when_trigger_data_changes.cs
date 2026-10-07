// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_trigger_data_changes : given.a_reactive_comparison
{
    string _propertyId = string.Empty;

    void Because()
    {
        CompareSnapshots(ReactiveSource, ReactiveSource.Replace("amount Decimal", "amount String", StringComparison.Ordinal));
        _propertyId = Workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Property && assignment.Address.OwnerKind == SemanticKind.Trigger).Id.ToString();
    }

    [Fact] void should_compare_the_trigger_using_its_catalog_id() => Items("members").Any(item => item.GetProperty("kind").GetString() == "Trigger" && item.GetProperty("semanticId").GetString() is not null).ShouldBeTrue();
    [Fact] void should_compare_the_assigned_trigger_property() => Items("members").Any(item => item.GetProperty("semanticId").GetString() == _propertyId && item.GetProperty("member").GetString() == "type").ShouldBeTrue();
    [Fact] void should_report_a_semantic_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
