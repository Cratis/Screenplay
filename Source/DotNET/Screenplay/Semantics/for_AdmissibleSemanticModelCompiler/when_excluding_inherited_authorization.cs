// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_inherited_authorization : given.security_documents
{
    void Establish() => Source(Policy + "module M\n  feature F\n    authorize Access\n    slice StateChange First\n      command Save\n    slice StateChange Second\n      command Update\n    feature Nested\n      slice StateChange Third\n        command Add\n  feature Other\n    slice StateChange Kept\n");
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_bind_the_safe_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_remove_every_protected_descendant() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["Access", "M/F/First", "M/F/Second", "M/F/Nested/Third"]);
    [Fact] void should_keep_the_other_feature() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_report_the_policy_as_root() => _result.Exclusions[0].Cause.ShouldEqual(SemanticExclusionCause.Root);
    [Fact] void should_report_every_protected_descendant_as_dependent() => _result.Exclusions.Skip(1).ShouldEachConformTo(value => value.Cause == SemanticExclusionCause.Dependent);
    [Fact] void should_name_the_inherited_policy() => _result.Exclusions.Skip(1).ShouldEachConformTo(value => value.DependsOn.SequenceEqual(new SemanticExclusionReference[] { new(SemanticExclusionKind.Policy, "Access") }));
}
