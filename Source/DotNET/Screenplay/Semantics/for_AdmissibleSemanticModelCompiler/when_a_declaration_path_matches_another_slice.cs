// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_a_declaration_path_matches_another_slice : given.documents
{
    void Establish() => Source("module M\n  feature F\n    slice StateView Nested\n      readmodel View\n        id String\n      query Payments => View[]\n        scoped to identity\n    feature Nested\n      slice StateChange Payments\n");
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_bind_the_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_exclude_only_the_enclosing_slice() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Nested"]);
    [Fact] void should_keep_the_slice_matching_the_query_path() => _result.Compilation.Value!.Model.Application.Modules.Single().Features.Single().Features.Single().Slices.Select(slice => slice.Name).ShouldEqual(["Payments"]);
}
