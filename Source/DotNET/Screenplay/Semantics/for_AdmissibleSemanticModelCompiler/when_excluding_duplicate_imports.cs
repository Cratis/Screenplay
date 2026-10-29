// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_duplicate_imports : given.documents
{
    void Establish() => Source("import External.View\nimport External.View\n" + Valid);
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_bind_the_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_keep_the_unrelated_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_preserve_both_import_exclusions() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["External.View", "External.View"]);
    [Fact] void should_preserve_source_order() => _result.Exclusions.Select(value => value.Location.Line).ShouldEqual([1, 2]);
    [Fact] void should_report_both_imports_as_roots() => _result.Exclusions.ShouldEachConformTo(value => value.Cause == SemanticExclusionCause.Root && value.DependsOn.IsEmpty);
}
