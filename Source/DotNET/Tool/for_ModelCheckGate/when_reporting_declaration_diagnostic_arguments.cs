// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_ModelCheckGate;

public class when_reporting_declaration_diagnostic_arguments : for_ModelCheck.given.a_model
{
    JsonDocument _report;
    JsonElement _subject;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), "concept Code : String\n  validate\n    matches \"[\"");
    void Because()
    {
        ModelCheck.Run([Root, "--format", "json"], Output, Error);
        _report = JsonDocument.Parse(Output.ToString());
        _subject = _report.RootElement.GetProperty("diagnostics").EnumerateArray().Single(item => item.GetProperty("code").GetString() == "PLAY0367").GetProperty("arguments").GetProperty("subject");
    }
    void Destroy() => _report.Dispose();
    [Fact] void should_expose_the_declaration_kind() => _subject.GetProperty("kind").GetString().ShouldEqual("declaration");
    [Fact] void should_expose_the_declaration_path() => _subject.GetProperty("value").GetProperty("path").EnumerateArray().Select(part => part.GetString()).ShouldEqual(["Code"]);
}
