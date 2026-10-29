// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_an_import : given.documents
{
    void Establish() => Source("import External.View\n" + Valid + "    slice StateView Imported\n      query All => View[]\n");
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_bind_the_subset() => Assert.True(_result.Compilation.Success, string.Join('\n', _result.Compilation.Diagnostics.Select(value => $"{value.Code}: {value.Message}")));
    [Fact] void should_remove_the_import_and_its_consumer() => _result.Exclusions.Select(value => value.Kind).ShouldEqual([SemanticExclusionKind.Import, SemanticExclusionKind.Slice]);
    [Fact] void should_name_the_import() => _result.Exclusions[0].Address.ShouldEqual("External.View");
    [Fact] void should_keep_the_independent_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_report_the_import_as_root() => _result.Exclusions[0].Cause.ShouldEqual(SemanticExclusionCause.Root);
    [Fact] void should_not_name_import_dependencies() => _result.Exclusions[0].DependsOn.ShouldBeEmpty();
    [Fact] void should_report_the_consumer_as_root_without_an_original_resolved_link() => _result.Exclusions[1].Cause.ShouldEqual(SemanticExclusionCause.Root);
    [Fact] void should_not_invent_an_import_link() => _result.Exclusions[1].DependsOn.ShouldBeEmpty();
}
