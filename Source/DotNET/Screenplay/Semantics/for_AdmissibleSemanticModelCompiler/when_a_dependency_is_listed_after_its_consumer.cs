// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_a_dependency_is_listed_after_its_consumer : given.documents
{
    void Establish() => Source("module M\n  feature F\n    slice StateView Consumer\n      query All => View[]\n    slice StateView Removed\n      readmodel View\n        id String\n      query Scoped => View[]\n        scoped to identity\n");
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_preserve_source_order() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Consumer", "M/F/Removed"]);
    [Fact] void should_report_the_consumer_as_dependent() => _result.Exclusions[0].Cause.ShouldEqual(SemanticExclusionCause.Dependent);
    [Fact] void should_name_the_later_exclusion() => _result.Exclusions[0].DependsOn.AsEnumerable().ShouldEqual([new(SemanticExclusionKind.Slice, "M/F/Removed")]);
    [Fact] void should_report_the_dependency_as_root() => _result.Exclusions[1].Cause.ShouldEqual(SemanticExclusionCause.Root);
}
