// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_ModelCheckGate;

public class when_reporting_json_with_open_questions : for_ModelCheck.given.a_model
{
    int _exitCode;
    JsonDocument _report;

    void Establish()
    {
        File.WriteAllText(Path.Combine(Root, "application.play"), "module M\n");
        File.WriteAllText(Path.Combine(Root, "screenplay.json"), "{\"validation\":{\"openQuestions\":\"fail\"}}");
        Directory.CreateDirectory(Path.Combine(Root, ".screenplay"));
        File.WriteAllText(Path.Combine(Root, ".screenplay", "notes.json"), "invalid notes");
    }

    void Because()
    {
        _exitCode = ModelCheck.Run([Root, "--format", "json"], Output, Error);
        _report = JsonDocument.Parse(Output.ToString());
    }

    void Destroy() => _report.Dispose();
    [Fact] void should_exit_could_not_run_for_unreadable_notes() => _exitCode.ShouldEqual(2);
    [Fact] void should_include_notes_and_their_refusal_in_the_json_document() => _report.RootElement.GetProperty("openQuestions").GetProperty("failureKind").GetString().ShouldEqual("UnreadableNotes");
    [Fact] void should_name_the_refusal_kind_in_camel_case() => _report.RootElement.GetProperty("refusal").GetProperty("kind").GetString().ShouldEqual("unreadableNotes");
    [Fact] void should_include_the_effective_notes_gate() => _report.RootElement.GetProperty("gate").GetProperty("openQuestions").GetProperty("value").GetString().ShouldEqual("fail");
    [Fact] void should_keep_stderr_empty() => Error.ToString().ShouldEqual(string.Empty);
}
