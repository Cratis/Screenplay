// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_excluding_a_trigger : given.documents
{
    const string Trigger = "trigger Changed\n  entry String\n  source\n";
    const string Watcher = "    slice Automation Watcher\n      reaction Sync\n        when Changed\n          entry\n          produces Synced\n            for entry\n            seen = entry\n      event Synced\n        seen String\n";

    void Establish() => Source(Trigger + Valid + Watcher);
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_fail_the_strict_binding_on_the_untyped_datum() => _normal.Diagnostics.ShouldContain(value => value.Severity == DiagnosticSeverity.Error && value.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_bind_the_subset() => Assert.True(_result.Compilation.Success, string.Join('\n', _result.Compilation.Diagnostics.Select(value => $"{value.Code}: {value.Message}")));
    [Fact] void should_remove_the_trigger_and_its_reaction_slice() => _result.Exclusions.Select(value => value.Kind).ShouldEqual([SemanticExclusionKind.Trigger, SemanticExclusionKind.Slice]);
    [Fact] void should_name_the_trigger_and_the_slice() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["Changed", "M/F/Watcher"]);
    [Fact] void should_report_the_trigger_as_root() => _result.Exclusions[0].Cause.ShouldEqual(SemanticExclusionCause.Root);
    [Fact] void should_not_name_trigger_dependencies() => _result.Exclusions[0].DependsOn.ShouldBeEmpty();
    [Fact] void should_keep_the_trigger_diagnostic() => _result.Exclusions[0].Diagnostics.ShouldContain(value => value.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_report_the_reaction_slice_as_dependent() => _result.Exclusions[1].Cause.ShouldEqual(SemanticExclusionCause.Dependent);
    [Fact] void should_name_the_trigger_as_the_dependency() => _result.Exclusions[1].DependsOn.AsEnumerable().ShouldEqual([new(SemanticExclusionKind.Trigger, "Changed")]);
    [Fact] void should_keep_the_independent_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_remove_the_trigger_from_the_model() => _result.Compilation.Value!.Model.Application.Triggers.ShouldBeEmpty();
    [Fact]
    void should_keep_metadata_deterministic() => _compiler.CompileAdmissible("Example", _documents).Exclusions.Select(value => (value.Kind, value.Address, value.Location, value.Cause))
        .ShouldEqual(_result.Exclusions.Select(value => (value.Kind, value.Address, value.Location, value.Cause)));
}
