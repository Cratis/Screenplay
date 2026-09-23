// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog.when_resolving_a_path;

public class and_it_is_empty : Specification
{
    EventContextPathResolution _empty;
    EventContextPathResolution _trailing;

    void Because()
    {
        _empty = EventContextCatalog.Resolve(string.Empty);
        _trailing = EventContextCatalog.Resolve("eventType.");
    }

    [Fact] void should_report_an_empty_path_as_missing() => _empty.Status.ShouldEqual(EventContextPathStatus.Missing);
    [Fact] void should_report_an_empty_segment_as_missing() => _trailing.Status.ShouldEqual(EventContextPathStatus.Missing);
}
