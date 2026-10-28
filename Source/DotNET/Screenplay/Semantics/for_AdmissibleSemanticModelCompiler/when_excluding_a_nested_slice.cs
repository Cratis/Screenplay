// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_a_nested_slice : given.documents
{
    void Establish() => Source("module M\n  feature F\n    slice StateChange Nested\n    feature Nested\n      slice StateView Payments\n        readmodel View\n          id String\n        query Scoped => View[]\n          scoped to identity\n");
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_bind_the_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_keep_the_slice_sharing_the_feature_name() => Slices.ShouldEqual(["Nested"]);
    [Fact] void should_exclude_only_the_nested_slice() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Nested/Payments"]);
    [Fact] void should_record_the_nested_feature_parent() => _result.Exclusions.Single().ParentId.ShouldEqual(_documents.IdentityCatalog.ResolveSemantic(SemanticAddress.ForFeature(_documents.IdentityCatalog.Application, "M", ["F", "Nested"])));
}
