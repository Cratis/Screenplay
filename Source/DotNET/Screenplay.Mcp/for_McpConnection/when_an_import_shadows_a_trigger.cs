// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_an_import_shadows_a_trigger : given.a_connection
{
    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "import Outside.Created\ntrigger Created\nmodule M\n  feature F\n    slice Automation Consumer\n      reaction React\n        when Created");
        Initialize();
    }

    [Fact]
    void should_not_find_the_imported_event_reference_for_the_trigger()
    {
        var result = Call("find-references", new { address = "Created", kind = "Trigger" }).GetProperty("result").GetProperty("structuredContent");
        result.GetProperty("references").GetArrayLength().ShouldEqual(0);
    }
}
