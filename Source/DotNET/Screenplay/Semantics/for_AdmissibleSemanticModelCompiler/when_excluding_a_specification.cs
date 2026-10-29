// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_a_specification : given.documents
{
    void Establish() => Source("module M\n  feature F\n    slice StateChange Kept\n      command Record\n        amount Int\n        produces Recorded\n          amount = amount\n      event Recorded\n        amount Int\n      specification Incomplete\n        when Record\n        then error \"invalid\"\n");
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_bind_the_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_keep_the_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_exclude_only_the_specification() => _result.Exclusions.Select(value => value.Kind).ShouldEqual([SemanticExclusionKind.Specification]);
    [Fact] void should_name_the_specification() => _result.Exclusions.Single().Address.ShouldEqual("M/F/Kept/Incomplete");
    [Fact] void should_keep_the_original_diagnostic() => _result.Exclusions.Single().Diagnostics.ShouldEqual(_normal.Diagnostics.Where(value => value.Severity == DiagnosticSeverity.Error));
    [Fact] void should_attach_evidence_to_the_parent_feature() => _result.Exclusions.Single().ParentId.ShouldEqual(_documents.IdentityCatalog.ResolveSemantic(SemanticAddress.ForFeature(_documents.IdentityCatalog.Application, "M", "F")));
    [Fact] void should_remove_the_specification_from_the_model() => _result.Compilation.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.ShouldBeEmpty();
}
