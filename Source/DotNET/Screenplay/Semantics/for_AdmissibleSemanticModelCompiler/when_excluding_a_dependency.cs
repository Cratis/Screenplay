// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_a_dependency : given.documents
{
    void Establish() => Source(Unsupported + "    slice StateView Dependent\n      query All => View[]\n");
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_bind_after_the_cascade() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_remove_the_dependent_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_record_both_passes() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Removed", "M/F/Dependent"]);
}
