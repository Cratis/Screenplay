// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

// Decision 0098 (#857): a list of objects crosses a context boundary whole, or one private event per element.
public class with_a_list_of_objects : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    const string Model = """
        type StockLine
          sku String
          quantity Int
        module Warehouse
          feature Reservations
            slice Translate Receive
              direction inbound
              event OrderConfirmed from "sales"
                orderId String
                lines StockLine[]
              event ReservationRequested
                orderId String
                lines StockLine[]
              event LineReserved
                orderId String
                sku String
                quantity Int
              capture Whole
                source events
                  from OrderConfirmed
                key orderId
                append ReservationRequested
                  when added
                    orderId = $.orderId
                    lines = $.lines
              capture PerLine
                source events
                  from OrderConfirmed
                key orderId
                children lines identified by sku
                  append LineReserved
                    when added
                      orderId = $.orderId
                      sku = $.sku
                      quantity = $.quantity
              specification ACopyOfTheWholeOrder
                when append OrderConfirmed
                  orderId = "o1"
                  lines = [{ "sku": "a", "quantity": 2 }, { "sku": "b", "quantity": 5 }]
                then ReservationRequested
                  orderId = "o1"
                  lines = [{ "sku": "a", "quantity": 2 }, { "sku": "b", "quantity": 5 }]
                then LineReserved
                  orderId = "o1"
                  sku = "a"
                  quantity = 2
                then LineReserved
                  orderId = "o1"
                  sku = "b"
                  quantity = 5
              specification ARepeatedOrderAddsNoLine
                given OrderConfirmed
                  orderId = "o1"
                  lines = [{ "sku": "a", "quantity": 2 }]
                when append OrderConfirmed
                  orderId = "o1"
                  lines = [{ "sku": "a", "quantity": 2 }, { "sku": "b", "quantity": 5 }]
                then LineReserved
                  orderId = "o1"
                  sku = "b"
                  quantity = 5
        """;

    SemanticSpecificationRun _whole;
    SemanticSpecificationRun _repeated;

    void Because()
    {
        Compile(Model);
        _whole = Run("ACopyOfTheWholeOrder");
        _repeated = Run("ARepeatedOrderAddsNoLine");
    }

    [Fact] void should_run_the_order_that_crosses_whole() => Assert.True(_whole.Passed, string.Join('\n', _whole.Failures));
    [Fact] void should_append_one_event_per_line_and_the_whole_copy() => _whole.Execution.World.Facts.Length.ShouldEqual(4);
    [Fact] void should_append_only_the_line_that_is_new() => Assert.True(_repeated.Passed, string.Join('\n', _repeated.Failures));
}
