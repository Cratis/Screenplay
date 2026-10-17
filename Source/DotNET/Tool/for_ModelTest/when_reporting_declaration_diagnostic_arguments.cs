// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_reporting_declaration_diagnostic_arguments : given.a_model
{
    const string Model = """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateView List
              readmodel Project
                projectId ProjectId
                name ProjectName
              query AllProjects => Project[]
                filter tenantId String from $context.tenant
        """;

    JsonDocument _report;
    JsonElement _subject;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), Model);

    void Because()
    {
        ModelTest.Run([Root, "--format", "json"], Output, Error);
        _report = JsonDocument.Parse(Output.ToString());
        _subject = _report.RootElement.GetProperty("diagnostics").EnumerateArray().First(item => item.GetProperty("code").GetString() == "PLAY0268" && item.GetProperty("arguments").TryGetProperty("subject", out var subject) && subject.GetProperty("value").GetProperty("kind").GetString() == "query").GetProperty("arguments").GetProperty("subject");
    }

    void Destroy() => _report.Dispose();

    [Fact] void should_expose_the_declaration_kind() => _subject.GetProperty("kind").GetString().ShouldEqual("declaration");
    [Fact] void should_expose_the_declaration_path() => _subject.GetProperty("value").GetProperty("path").EnumerateArray().Select(part => part.GetString()).ShouldEqual(["Projects", "Registration", "List", "AllProjects"]);
}
