// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpRepairCapabilities.when_reading_repair_capabilities;

public class and_refactorings_are_advertised : given.repair_capabilities
{
    [Fact] void should_advertise_the_extraction_tool() => Refactoring("extract-inline-event").GetProperty("tool").GetString().ShouldEqual("propose-extract-inline-event");
    [Fact] void should_require_canonical_formatting_for_extraction() => Refactoring("extract-inline-event").GetProperty("requiredFormatting").GetString().ShouldEqual("CanonicalizeTouchedDocuments");
    [Fact] void should_advertise_the_rename_tool() => Refactoring("rename-declaration").GetProperty("tool").GetString().ShouldEqual("propose-rename");
    [Fact] void should_preserve_event_pins_by_default() => Refactoring("rename-declaration").GetProperty("eventPinDefault").GetString().ShouldEqual("Preserve");
    [Fact] void should_make_never_persisted_events_an_opt_in() => Refactoring("rename-declaration").GetProperty("eventNeverPersistedOptIn").GetBoolean().ShouldBeTrue();
    [Fact] void should_discover_them_through_the_refactorings_view() => Capabilities.GetProperty("refactorings").EnumerateArray().All(refactoring => refactoring.GetProperty("discoveryView").GetString() == "refactorings").ShouldBeTrue();
    [Fact] void should_keep_them_out_of_fix_all() => Capabilities.GetProperty("refactorings").EnumerateArray().All(refactoring => !refactoring.GetProperty("canFixAll").GetBoolean()).ShouldBeTrue();
}
