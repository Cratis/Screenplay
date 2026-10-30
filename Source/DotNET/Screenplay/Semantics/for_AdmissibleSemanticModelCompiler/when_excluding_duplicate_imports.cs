// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_duplicate_imports : given.documents
{
    void Establish() => Source("import External.View\nimport External.View\n" + Valid);
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_bind_the_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_keep_the_unrelated_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_exclude_the_merged_import_once() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["External.View"]);
    [Fact] void should_preserve_the_first_import_location() => _result.Exclusions.Select(value => value.Location.Line).ShouldEqual([1]);
    [Fact] void should_keep_metadata_deterministic() => _compiler.CompileAdmissible("Example", _documents).Exclusions.Select(value => (value.Kind, value.Address, value.Location, value.Cause))
        .ShouldEqual(_result.Exclusions.Select(value => (value.Kind, value.Address, value.Location, value.Cause)));
    [Fact] void should_report_the_import_as_root() => _result.Exclusions.ShouldEachConformTo(value => value.Cause == SemanticExclusionCause.Root && value.DependsOn.IsEmpty);
}
