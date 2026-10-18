// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_snapshot_input_order_differs : given.a_compiled_model
{
    string _first = null!;
    string _second = null!;

    void Establish()
    {
        _snapshot.Appends.Add(new(new("Extra", 3), 1));
        _snapshot.Edges.Add(new(Register, new("Extra", 3), 1));
        _snapshot.Registered.Add(new("OnlyRegistered", 2));
    }

    void Because()
    {
        _first = ObservationReportJson.Write(Compare());
        _snapshot.Registered.Reverse();
        _snapshot.Appends.Reverse();
        _snapshot.Edges.Reverse();
        _second = ObservationReportJson.Write(Compare());
    }

    [Fact] void should_produce_identical_report_bytes() => _second.ShouldEqual(_first);
}
