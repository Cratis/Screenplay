// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_a_protected_read_model : given.security_documents
{
    void Establish() => Source(AdmittedProtected.Replace("        by id String\n", "        by id String\n        scoped to identity\n", StringComparison.Ordinal));
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_have_a_strict_binding_failure() => _normal.Success.ShouldBeFalse();
    [Fact] void should_bind_the_safe_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_remove_the_view_and_every_reader() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Write", "M/F/Lookup", "M/F/Inspect"]);
    [Fact] void should_keep_only_the_unrelated_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_report_the_failing_view_as_root() => _result.Exclusions[1].Cause.ShouldEqual(SemanticExclusionCause.Root);
    [Fact] void should_not_name_dependencies_for_the_root_view() => _result.Exclusions[1].DependsOn.ShouldBeEmpty();
    [Fact] void should_report_the_forced_readers_as_dependent() => _result.Exclusions.Where(value => value.Address != "M/F/Lookup").ShouldEachConformTo(value => value.Cause == SemanticExclusionCause.Dependent);
    [Fact] void should_name_the_view_that_forced_the_readers() => _result.Exclusions.Where(value => value.Address != "M/F/Lookup").ShouldEachConformTo(value => value.DependsOn.SequenceEqual(new SemanticExclusionReference[] { new(SemanticExclusionKind.Slice, "M/F/Lookup") }));
    [Fact] void should_record_the_protected_read_closure() => _result.Exclusions.ShouldEachConformTo(value => value.Reason?.Contains("View", StringComparison.Ordinal) == true);
}
