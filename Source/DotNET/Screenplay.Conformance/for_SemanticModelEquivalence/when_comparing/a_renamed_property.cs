// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class a_renamed_property : given.compiled_models
{
    const string Properties = "/application/modules[Projects]/features[Registration]/slices[List]/readModels[Projects]/properties";

    void Because() => _comparison = SemanticModelEquivalence.Compare(Compile(Source), Compile(Changed("name String\n        projectId Uuid", "title String\n        projectId Uuid")));

    [Fact] void should_report_two_differences() => _comparison.Differences.Length.ShouldEqual(2);
    [Fact] void should_report_the_old_name_as_missing() => _comparison.Differences.Single(_ => _.Kind == SemanticDifferenceKind.Missing).Path.ShouldEqual($"{Properties}[name]");
    [Fact] void should_report_the_new_name_as_unexpected() => _comparison.Differences.Single(_ => _.Kind == SemanticDifferenceKind.Unexpected).Path.ShouldEqual($"{Properties}[title]");
}
