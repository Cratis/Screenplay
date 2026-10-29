// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_a_policy : given.security_documents
{
    void Establish() => Source(Policy + Authorized);
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_have_a_strict_binding_failure() => _normal.Success.ShouldBeFalse();
    [Fact] void should_bind_the_safe_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_exclude_the_policy_and_both_protected_slices() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["Access", "M/F/Write", "M/F/Lookup"]);
    [Fact] void should_keep_only_the_unrelated_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_remove_the_policy() => _result.Compilation.Value!.Model.Application.Policies.ShouldBeEmpty();
    [Fact] void should_explain_the_security_closure() => _result.Exclusions.ShouldEachConformTo(value => value.Reason?.Contains("Access", StringComparison.Ordinal) == true);
}
