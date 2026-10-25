// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class a_changed_type : given.compiled_models
{
    void Because() => _comparison = SemanticModelEquivalence.Compare(Compile(Source), Compile(Changed("name String\n        projectId Uuid", "name Decimal\n        projectId Uuid")));

    [Fact] void should_report_a_difference() => _comparison.IsEquivalent.ShouldBeFalse();
    [Fact] void should_report_only_changes() => _comparison.Differences.All(_ => _.Kind == SemanticDifferenceKind.Changed).ShouldBeTrue();
    [Fact] void should_report_the_property() => _comparison.Differences.All(_ => _.Path.StartsWith("/application/modules[Projects]/features[Registration]/slices[List]/readModels[Projects]/properties[name]/", StringComparison.Ordinal)).ShouldBeTrue();
}
