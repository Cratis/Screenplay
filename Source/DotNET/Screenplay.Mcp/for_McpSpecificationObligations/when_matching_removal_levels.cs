// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationObligations;

public class when_matching_removal_levels : for_McpConnection.given.a_connection
{
    JsonElement[] _items = [];

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            type Line
              id Uuid
            module Billing
              feature Invoices
                slice StateView Details
                  event LineRemoved
                  event OtherLineRemoved
                  event InvoiceRemoved
                  readmodel InvoiceDetails
                    id Uuid identifier
                    items Line[]
                  projection InvoiceDetails
                    children items identified by id
                      remove with LineRemoved
                      remove with OtherLineRemoved
                    remove with InvoiceRemoved
                  specification ChildRemoved
                    when append LineRemoved
                    then readmodel InvoiceDetails
                      items = []
                  specification WrongLevel
                    when append OtherLineRemoved
                    then no readmodel InvoiceDetails for "invoice"
                  specification RootStillPresent
                    when append InvoiceRemoved
                    then readmodel InvoiceDetails
                      items = []
                slice StateView Variants
                  event Activated
                  event Archived
                  event Removed
                  event ArchiveRemoved
                  readmodel Active
                    id Uuid identifier
                  readmodel Archive
                    id Uuid identifier
                  projection Lifecycle
                    variant Active
                      enters on Activated
                      remove with Removed
                    variant Archive
                      enters on Archived
                      remove with ArchiveRemoved
                  specification RightVariant
                    when append ArchiveRemoved
                    then no readmodel Archive for "invoice"
                  specification WrongVariant
                    when append Removed
                    then no readmodel Archive for "invoice"
            """);
        Initialize();
    }

    void Because() => _items = [.. Call("find-specification-obligations").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray().Where(item => item.GetProperty("ruleId").GetString() == "SPEC007")];

    [Fact] void should_match_the_child_collection_assertion() => Item("LineRemoved").GetProperty("status").GetString().ShouldEqual("met");
    [Fact] void should_not_match_child_removal_to_root_absence() => Item("OtherLineRemoved").GetProperty("status").GetString().ShouldEqual("unmet");
    [Fact] void should_match_absence_of_the_owning_variant() => Item("ArchiveRemoved").GetProperty("status").GetString().ShouldEqual("met");
    [Fact] void should_not_match_root_removal_to_a_collection_assertion() => Item("InvoiceRemoved").GetProperty("status").GetString().ShouldEqual("unmet");
    [Fact] void should_not_match_absence_of_a_different_variant() => Item("Removed").GetProperty("status").GetString().ShouldEqual("unmet");

    JsonElement Item(string subject) => _items.Single(item => item.GetProperty("subject").GetString() == subject);
}
