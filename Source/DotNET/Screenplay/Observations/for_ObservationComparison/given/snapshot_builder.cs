// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.given;

public class snapshot_builder
{
    public ObservedSequenceCoverage Coverage { get; set; } = new("log", true, 0, 1, 1, []);
    public List<ObservedEventType> Registered { get; set; } = [];
    public List<ObservedAppend> Appends { get; set; } = [];
    public List<ObservedEdge> Edges { get; set; } = [];
    public List<UnattributedAppend> Unattributed { get; set; } = [];
    public List<ObservedConsumer> Consumers { get; set; } = [];

    public ObservationSnapshot Build() => ObservationSnapshot.Create("Projects", "tenant", Coverage, Registered, Appends, Edges, Unattributed, Consumers, new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
}
