// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.given;

public class a_record_capture : a_v6_scenario
{
    const string Source =
        """
        module Billing
          feature Import
            slice Translate Records
              capture Records
                source api
                  api LegacyApi
                  route /records
                key id
                map
                  status = sourceStatus translate
                    "sendt" => sent
                    "betalt" => paid
                append Added
                  when added
                    status = $.status
                append Changed
                  when status
                append AnyChanged
                  when status or label
                append AllChanged
                  when status and label
                append Transitioned
                  when status from "sent" to "paid"
                children items identified by id
                  append ChildAdded
                    when added
                  append ChildChanged
                    when value
                  append ChildAnyChanged
                    when value or id
                  append ChildAllChanged
                    when value and id
                  append ChildTransitioned
                    when value from "before" to "after"
                nested contact
                  append NestedAdded
                    when added
                  append NestedChanged
                    when value
                  append NestedAnyChanged
                    when value or label
                  append NestedAllChanged
                    when value and label
                  append NestedTransitioned
                    when value from "before" to "after"
              event Added
                status String
              event Changed
              event AnyChanged
              event AllChanged
              event Transitioned
              event ChildAdded
              event ChildChanged
              event ChildAnyChanged
              event ChildAllChanged
              event ChildTransitioned
              event NestedAdded
              event NestedChanged
              event NestedAnyChanged
              event NestedAllChanged
              event NestedTransitioned
        """;

    protected void CompileScenario(string scenario) => Compile($"{Source}\n{scenario}");
}
