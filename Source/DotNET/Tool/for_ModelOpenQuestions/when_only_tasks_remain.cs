// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.for_ModelOpenQuestions;

public class when_only_tasks_remain : given.a_model_with_questions
{
    int _exit;

    void Establish()
    {
        var notes = ModelNotesStore.ReadBytes(File.ReadAllBytes(NotesPath), ApplicationIdentity.Create("Model")).Document!;
        File.WriteAllBytes(NotesPath, ModelNotesStore.Serialize(notes with { Notes = [.. notes.Notes.Select(note => note with { Kind = "task" })] }));
    }

    void Because() => _exit = ModelCheck.Run([Root, "--open-questions", "fail"], Output, Error);

    [Fact] void should_not_gate_on_tasks() => _exit.ShouldEqual(0);
    [Fact] void should_report_zero_questions() => Output.ToString().Contains("0 open question(s)", StringComparison.Ordinal).ShouldBeTrue();
}
