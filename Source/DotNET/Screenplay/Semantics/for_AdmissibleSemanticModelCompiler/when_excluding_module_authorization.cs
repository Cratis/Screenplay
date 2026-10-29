// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_module_authorization : given.security_documents
{
    void Establish() => Source(Policy + "module M\n  authorize Access\n  feature F\n    slice StateChange First\n      command Save\n    feature Nested\n      slice StateChange Second\n        command Update\n  feature Other\n    slice StateChange Third\n      command Add\nmodule Other\n  feature F\n    slice StateChange Kept\n");
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_have_a_strict_binding_failure() => _normal.Success.ShouldBeFalse();
    [Fact] void should_bind_the_safe_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_remove_every_protected_descendant() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["Access", "M/F/First", "M/F/Nested/Second", "M/Other/Third"]);
    [Fact] void should_keep_the_other_module() => Slices.ShouldEqual(["Kept"]);
}
