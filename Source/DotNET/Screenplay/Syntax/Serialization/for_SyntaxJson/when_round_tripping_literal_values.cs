// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_round_tripping_literal_values : Specification
{
    readonly object?[] _values = [null, "quoted \"text\"\nnext", true, false, 0d, -2.5d, double.MaxValue, 7, long.MaxValue, 2.25f, decimal.MaxValue, 0.0000000000000000000000000001m];
    object?[] _roundTrips;

    void Because() => _roundTrips = [.. _values.Select(value =>
        ((LiteralExpressionSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(new LiteralExpressionSyntax(value, SourceLocation.Start)))).Value)];

    [Fact] void should_cover_every_supported_literal_type() => _roundTrips.Length.ShouldEqual(12);
    [Fact] void should_preserve_values_without_precision_loss() => _values.SequenceEqual(_roundTrips).ShouldBeTrue();
    [Fact] void should_preserve_the_actual_clr_types() => _values.Select(value => value?.GetType()).SequenceEqual(_roundTrips.Select(value => value?.GetType())).ShouldBeTrue();
}
