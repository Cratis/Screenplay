// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_a_chain_of_dependencies : given.documents
{
    void Establish() => Source(Unsupported + "    slice StateView Middle\n      readmodel Summary\n        summaryId String\n      query Own => Summary[]\n      query All => View[]\n    slice StateView Last\n      query Summaries => Summary[]\n");
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_bind_the_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_keep_only_the_unrelated_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_exclude_the_whole_chain() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Removed", "M/F/Middle", "M/F/Last"]);
    [Fact] void should_report_the_first_link_as_root() => _result.Exclusions[0].Cause.ShouldEqual(SemanticExclusionCause.Root);
    [Fact] void should_report_the_later_links_as_dependent() => _result.Exclusions.Skip(1).Select(value => value.Cause).ShouldEqual([SemanticExclusionCause.Dependent, SemanticExclusionCause.Dependent]);
    [Fact] void should_name_the_root_for_the_middle_link() => _result.Exclusions[1].DependsOn.AsEnumerable().ShouldEqual([new(SemanticExclusionKind.Slice, "M/F/Removed")]);
    [Fact] void should_name_only_the_immediate_dependency_for_the_last_link() => _result.Exclusions[2].DependsOn.AsEnumerable().ShouldEqual([new(SemanticExclusionKind.Slice, "M/F/Middle")]);
}
