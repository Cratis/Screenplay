// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_comparing_calendar_dates : Specification
{
    int _result;

    void Because() => _result = SemanticDateComparisons.Compare(SemanticValue.Text("2026-01-01"), SemanticValue.Text("2026-01-02"), SemanticPrimitiveType.Date);

    [Fact] void should_order_calendar_dates() => _result.ShouldBeLessThan(0);
}
