// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_an_event_generation_before_lineage_admission : given.a_semantic_binder
{
    const string Source =
        "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n" +
        "      event ProjectRegistered generation 1\n        projectId Uuid\n" +
        "      event ProjectRegistered generation 2\n        name String\n";

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_fail_closed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_only_one_precise_error()
    {
        var diagnostic = _result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.UnsupportedEventGenerationSemantics);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostic.Message.ShouldEqual("Event 'ProjectRegistered' cannot bind: generation lineage in the executable model is not yet available");
    }

    [Fact]
    void should_reject_a_single_explicit_generation_one()
    {
        var result = Bind("module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event ProjectRegistered generation 1\n        name String\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedEventGenerationSemantics);
    }

    [Fact]
    void should_reject_an_unmarked_first_generation_followed_by_a_marked_second()
    {
        var result = Bind("module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event ProjectRegistered\n        projectId Uuid\n      event ProjectRegistered generation 2\n        name String\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedEventGenerationSemantics);
    }

    [Fact]
    void should_reject_generations_in_two_slices_with_only_one_lineage_error()
    {
        var result = Bind(Source + "    slice StateChange ImportProject\n      event Imported generation 1\n        name String\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedEventGenerationSemantics).ShouldEqual(1);
    }

    [Fact]
    void should_leave_unmarked_events_bindable()
    {
        var result = Bind("module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event ProjectRegistered\n        name String\n");
        result.Success.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
    }
}
