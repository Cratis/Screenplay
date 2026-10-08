// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_selecting_an_unknown_scope : given.a_model
{
    ScopedDiagnosticResult? _result;

    void Because() => _result = ScopedDiagnostics.Select(Sources, "orders.Registration.Add");

    [Fact] void should_not_report_a_vacuous_success() => _result.ShouldBeNull();
}
