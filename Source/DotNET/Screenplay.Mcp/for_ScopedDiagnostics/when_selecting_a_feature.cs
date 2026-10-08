// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_selecting_a_feature : given.a_model
{
    ScopedDiagnosticResult _result;

    void Because() => _result = ScopedDiagnostics.Select(Sources, "Orders.Registration")!;

    [Fact] void should_count_the_feature_and_its_descendants() => _result.DeclarationCount.ShouldEqual(5);
    [Fact] void should_include_feature_errors() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("UnknownSibling", StringComparison.Ordinal)).ShouldBeTrue();
}
