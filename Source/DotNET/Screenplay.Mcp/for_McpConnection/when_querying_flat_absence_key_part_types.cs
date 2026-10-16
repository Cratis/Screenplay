// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_querying_flat_absence_key_part_types : given.a_connection
{
    JsonElement _result;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        concept InvoiceId : String
        concept PaymentMethod : Enum
          card
          cash
        module Billing
          feature Payments
            slice StateView Totals
              event Tick
              readmodel Row
                @for InvoiceId key
                method PaymentMethod key
              specification Missing
                when append Tick
                then no readmodel Row for {"for":"INV-1","method":"cash"}
        """);

    void Because()
    {
        var snapshot = new McpSnapshot(Root.Read());
        _result = JsonSerializer.SerializeToElement(McpFixtureQueries.Values(snapshot, null, "thenAbsentReadModelKeyPart", null, null), McpJson.Options);
    }

    [Fact] void should_return_both_flat_parts() => Items.GetArrayLength().ShouldEqual(2);
    [Fact] void should_type_a_part_named_for_instead_of_treating_it_as_the_whole_key() => Items.EnumerateArray().Single(item => item.GetProperty("property").GetString() == "for").GetProperty("declaredType").GetProperty("name").GetString().ShouldEqual("InvoiceId");
    [Fact] void should_type_the_enum_part_from_its_own_declaration() => Items.EnumerateArray().Single(item => item.GetProperty("property").GetString() == "method").GetProperty("declaredType").GetProperty("name").GetString().ShouldEqual("PaymentMethod");

    JsonElement Items => _result.GetProperty("page").GetProperty("items");
}
