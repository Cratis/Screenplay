// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_a_protected_read_model : given.security_documents
{
    void Establish() => Source("command reads protected\nmodule M\n  feature F\n    slice StateChange Write\n      command Save\n        id String identifier\n        reads View by id\n    slice StateView Lookup\n      readmodel View\n        id String key\n      query Scoped => View optional\n        by id String\n        scoped to identity\n    slice StateChange Inspect\n      command Inspect\n        id String identifier\n        reads View by id\n    slice StateChange Kept\n");
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_have_a_strict_binding_failure() => _normal.Success.ShouldBeFalse();
    [Fact] void should_bind_the_safe_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_remove_the_view_and_every_reader() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Write", "M/F/Lookup", "M/F/Inspect"]);
    [Fact] void should_keep_only_the_unrelated_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_record_the_protected_read_closure() => _result.Exclusions.ShouldEachConformTo(value => value.Reason?.Contains("View", StringComparison.Ordinal) == true);
}
