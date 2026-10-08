// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_querying_specification_routes;

public class and_routes_state_composite_parts : given.a_connection
{
    [Theory]
    [InlineData("givenEventStreamIdPart")]
    [InlineData("whenAppendedEventStreamIdPart")]
    [InlineData("thenEventStreamIdPart")]
    void should_expose_each_named_literal_without_joining_it(string role)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "eventsource A\n  identifier String\n  stream S\n    streamId\n      one String\n      two String\nmodule M\n  feature F\n    slice StateView S\n      event E\n      specification X\n        given E\n          for \"id\"\n          stream A.S\n            streamId\n              two = \"b\"\n              one = \"a\"\n        when append E\n          for \"id\"\n          stream A.S\n            streamId\n              two = \"b\"\n              one = \"a\"\n        then E\n          stream A.S\n            streamId\n              two = \"b\"\n              one = \"a\"");
        Initialize();
        var content = Call("find-fixtures", new { role }).GetProperty("result").GetProperty("structuredContent");
        var rows = content.GetProperty("page").GetProperty("items");
        rows.GetArrayLength().ShouldEqual(2);
        rows[0].GetProperty("property").GetString().ShouldEqual("two");
        rows[0].GetProperty("value").GetString().ShouldEqual("b");
        rows[1].GetProperty("property").GetString().ShouldEqual("one");
        rows[1].GetProperty("value").GetString().ShouldEqual("a");
    }
}
