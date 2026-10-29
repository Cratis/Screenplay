// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_a_slice : given.documents
{
    void Establish() => Source(Unsupported);
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_bind_the_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_keep_the_other_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_exclude_the_failing_slice() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Removed"]);
    [Fact] void should_record_the_kind() => _result.Exclusions.Single().Kind.ShouldEqual(SemanticExclusionKind.Slice);
    [Fact] void should_preserve_strict_failure() => _normal.Success.ShouldBeFalse();
    [Fact] void should_report_a_root_cause() => _result.Exclusions.Single().Cause.ShouldEqual(SemanticExclusionCause.Root);
    [Fact] void should_not_name_dependencies() => _result.Exclusions.Single().DependsOn.ShouldBeEmpty();
}
