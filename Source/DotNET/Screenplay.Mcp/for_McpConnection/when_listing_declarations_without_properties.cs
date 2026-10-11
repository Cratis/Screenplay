// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_listing_declarations_without_properties : given.a_connection
{
    JsonElement _described;
    JsonElement _searched;
    JsonElement _found;

    void Establish() => Initialize();
    void Because()
    {
        _described = Call("describe-application", new { view = "declarations", kind = "Property" }).GetProperty("result").GetProperty("structuredContent");
        _searched = Call("search-declarations", new { kind = "Property" }).GetProperty("result").GetProperty("structuredContent");
        _found = Call("find-declaration", new { name = "name", kind = "Property" }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_not_describe_members_as_declarations() => _described.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(0);
    [Fact] void should_not_search_members_as_declarations() => _searched.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(0);
    [Fact] void should_not_find_members_as_declarations() => _found.GetProperty("matches").GetArrayLength().ShouldEqual(0);
}
