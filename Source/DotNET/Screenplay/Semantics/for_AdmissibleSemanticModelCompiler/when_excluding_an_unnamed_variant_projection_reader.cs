// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_an_unnamed_variant_projection_reader : given.security_documents
{
    void Establish() => Source(UnnamedVariantProjection);
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_bind_the_safe_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_remove_the_variant_view_and_every_reader() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Write", "M/F/Lookup", "M/F/Inspect"]);
    [Fact] void should_keep_only_the_unrelated_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_report_the_readers_own_errors_as_roots() => _result.Exclusions.Where(value => value.Address != "M/F/Lookup").ShouldEachConformTo(value => value.Cause == SemanticExclusionCause.Root && value.DependsOn.IsEmpty);
    [Fact] void should_report_the_closed_view_as_dependent() => _result.Exclusions.Single(value => value.Address == "M/F/Lookup").Cause.ShouldEqual(SemanticExclusionCause.Dependent);
    [Fact] void should_name_the_readers_that_closed_the_view() => _result.Exclusions.Single(value => value.Address == "M/F/Lookup").DependsOn.AsEnumerable().ShouldEqual([new(SemanticExclusionKind.Slice, "M/F/Write"), new(SemanticExclusionKind.Slice, "M/F/Inspect")]);
}
