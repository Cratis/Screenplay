// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelCompiler.when_selecting_versions_with_refused_specifications;

public class with_specification_only_usage : given.a_specification_refusal_compiler
{
    const string VersionSource = """
        module M
          feature F
            slice StateChange Recording
              event Recorded
                amount Int
              command Record
                amount Int
                produces Recorded
                  amount = amount
              specification RecordingWorks
                when Record amount = 10
                then Recorded amount = 10
              specification Healthy
                when Record amount = 20
                then Recorded amount = 20
        """;

    [Theory]
    [InlineData("then Recorded amount = 10", "then no readmodel Missing for \"one\"")]
    [InlineData("when Record amount = 10", "given clock \"2026-10-05T08:00:00Z\"\n        when Record")]
    [InlineData("when Record amount = 10", "given generated Unknown.id = \"one\"\n        when Record amount = 10")]
    void should_ignore_version_usage_from_a_refused_specification(string before, string after)
    {
        var baseline = Compile(VersionSource);
        baseline.Success.ShouldBeTrue();
        var result = Compile(VersionSource.Replace(before, after, StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        Assert.True(result.SpecificationRefusedValue is not null, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.SpecificationRefusedValue!.Model.SemanticVersion.ShouldEqual(baseline.Value!.Model.SemanticVersion);
        result.SpecificationRefusedValue.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
        Recording(result.SpecificationRefusedValue).Specifications.Select(specification => specification.Name).ShouldContainOnly("Healthy");
        result.SpecificationRefusedValue.RefusedSpecifications.Single().Id.ShouldEqual(Recording(baseline.Value).Specifications.Single(specification => specification.Name == "RecordingWorks").Id);
        result.Diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldEqual(result.SpecificationRefusedValue.RefusedSpecifications.Single().Diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    void should_keep_version_usage_from_a_retained_specification()
    {
        var source = VersionSource.Replace("      specification RecordingWorks", "      readmodel State\n        amount Int\n      query StateById => State optional\n        by amount Int\n      projection States => State\n        remove with Recorded key amount\n      specification RecordingWorks", StringComparison.Ordinal)
            .Replace("then Recorded amount = 10", "then no readmodel State for 10", StringComparison.Ordinal);
        var result = Compile(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Value.ShouldNotBeNull();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V5);
        result.SpecificationRefusedValue.ShouldBeNull();
    }
}
