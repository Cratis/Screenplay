// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class catalog_revision_shaped_literals : given.compiled_models
{
    void Because() => _comparison = SemanticModelEquivalence.Compare(
        Compile(Changed("First", $"catrev1:{new string('0', 64)}")),
        Compile(Changed("First", $"catrev1:{new string('1', 64)}")));

    [Fact] void should_preserve_the_literal_difference() => _comparison.IsEquivalent.ShouldBeFalse();
    [Fact] void should_report_each_changed_specification_value() => _comparison.Differences.Length.ShouldEqual(3);
}
