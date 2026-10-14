// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_equivalent_structured_content : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    SemanticSpecificationRun _run;

    void Because()
    {
        Compile("""
            type Before
              label String
            type After
              label String
            module M
              feature F
                slice Translate Receive
                  direction inbound
                  event Arrived from "store"
                    id String
                    payload Before
                    items Before[]
                  event Updated from "store"
                    id String
                    payload After
                    items After[]
                  event Local
                  capture Feed
                    source events
                      from Arrived
                      from Updated
                    key id
                    append Local
                  specification S
                    given Arrived
                      id = "parcel"
                      payload = {"label":"same"}
                      items = [{"label":"same"}]
                    when append Updated
                      id = "parcel"
                      payload = {"label":"same"}
                      items = [{"label":"same"}]
                    then no events
            """);
        _run = Run("S");
    }

    [Fact] void should_compare_content_names_not_type_specific_property_identities() => _run.Passed.ShouldBeTrue();
}
