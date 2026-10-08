// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_validating_an_unknown_scope_through_the_public_api : given.a_model
{
    bool _resolved;
    ScopedDiagnosticResult? _result;
    ScopeSelectionError? _error;

    void Because() => _resolved = ScopedDiagnostics.TryValidate(Sources, "orders.Registration.Add", CompletenessChecks.None, out _result, out _error);

    [Fact] void should_refuse_the_selection() => _resolved.ShouldBeFalse();
    [Fact] void should_not_return_an_empty_successful_selection() => _result.ShouldBeNull();
    [Fact] void should_distinguish_an_unknown_scope() => _error!.Kind.ShouldEqual(ScopeSelectionErrorKind.UnknownScope);
    [Fact] void should_preserve_the_usage_message() => _error!.Message.ShouldEqual("Unknown scope 'orders.Registration.Add'. Expected a module, feature or slice address.");
}
