// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_a_dependent_also_has_its_own_error : given.documents
{
    void Establish() => Source(Unsupported + "    slice StateView Dependent\n      query All => View[]\n        scoped to identity\n");
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_exclude_both_slices() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Removed", "M/F/Dependent"]);
    [Fact] void should_report_both_own_errors_as_roots() => _result.Exclusions.Select(value => value.Cause).ShouldEqual([SemanticExclusionCause.Root, SemanticExclusionCause.Root]);
    [Fact] void should_not_name_dependencies_for_the_consumer() => _result.Exclusions[1].DependsOn.ShouldBeEmpty();
}
