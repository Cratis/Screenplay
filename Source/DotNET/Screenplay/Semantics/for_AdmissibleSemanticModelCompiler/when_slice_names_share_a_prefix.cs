// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_slice_names_share_a_prefix : given.documents
{
    void Establish() => Source("module M\n  feature F\n    slice StateChange Pay\n    slice StateView Payments\n      readmodel View\n        id String\n      query Scoped => View[]\n        scoped to identity\n");
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_bind_the_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_keep_the_prefix_sibling() => Slices.ShouldEqual(["Pay"]);
    [Fact] void should_exclude_only_the_enclosing_slice() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Payments"]);
}
