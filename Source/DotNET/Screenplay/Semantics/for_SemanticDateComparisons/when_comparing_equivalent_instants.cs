// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_comparing_equivalent_instants : Specification
{
    int _result;

    void Because() => _result = SemanticDateComparisons.Compare(SemanticValue.Text("2026-01-01T02:00:00.1234567+02:00"), SemanticValue.Text("2026-01-01T00:00:00.1234567Z"), SemanticPrimitiveType.DateTime);

    [Fact] void should_compare_utc_ticks_rather_than_spellings() => _result.ShouldEqual(0);
}
