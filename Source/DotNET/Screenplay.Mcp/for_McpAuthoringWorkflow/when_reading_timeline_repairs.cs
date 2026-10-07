// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reading_timeline_repairs : given.an_authoring_connection
{
    internal const string Source = "module M\n  feature Consumer\n    slice StateView View\n      readmodel Items\n        id Uuid\n      query FindItems => Items optional\n        by id Uuid\n      projection ItemsProjection => Items\n        from E key id\n  feature Producer\n    slice StateChange Write\n      event E\n        id Uuid\n";

    [Fact]
    void should_expose_the_reference_subject_and_a_typed_move_without_writing()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        var opened = Open();
        var repairs = Page("repairs", opened.GetProperty("revision").GetString()!);
        var repair = repairs.EnumerateArray().Single();
        repair.GetProperty("diagnosticCode").GetString().ShouldEqual("PLAY0516");
        repair.GetProperty("location").GetProperty("line").GetInt32().ShouldEqual(9);
        repair.GetProperty("subject").GetProperty("path").GetString()!.ShouldContain("/events/0");
        repair.GetProperty("operations")[0].GetProperty("operation").GetString().ShouldEqual("move");
        repair.GetProperty("canFixAll").GetBoolean().ShouldBeTrue();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    }
}
