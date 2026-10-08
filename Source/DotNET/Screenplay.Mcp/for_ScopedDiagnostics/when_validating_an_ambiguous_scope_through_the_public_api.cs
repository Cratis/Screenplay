// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_validating_an_ambiguous_scope_through_the_public_api : Specification
{
    bool _resolved;
    ScopedDiagnosticResult? _result;
    ScopeSelectionError? _error;

    void Because() => _resolved = ScopedDiagnostics.TryValidate(
        new Dictionary<string, string> { ["application.play"] = "module M\n  feature F\n    slice StateChange Duplicate\n    slice StateChange Duplicate" },
        "M.F.Duplicate",
        CompletenessChecks.None,
        out _result,
        out _error);

    [Fact] void should_refuse_the_selection() => _resolved.ShouldBeFalse();
    [Fact] void should_not_merge_the_matching_nodes() => _result.ShouldBeNull();
    [Fact] void should_distinguish_an_ambiguous_scope() => _error!.Kind.ShouldEqual(ScopeSelectionErrorKind.AmbiguousScope);
    [Fact] void should_preserve_the_usage_message() => _error!.Message.ShouldEqual("Ambiguous scope 'M.F.Duplicate'. Expected exactly one module, feature or slice.");
}
