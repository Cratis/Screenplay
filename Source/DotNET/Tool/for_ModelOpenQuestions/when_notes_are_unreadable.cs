// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelOpenQuestions;

public class when_notes_are_unreadable : given.a_model_with_questions
{
    int _exit;
    int _ordinaryExit;

    void Establish() => File.WriteAllText(NotesPath, "{");

    void Because()
    {
        _exit = ModelCheck.Run([Root, "--open-questions", "report"], Output, Error);
        _ordinaryExit = ModelCheck.Run([Root, "--warnaserror"], Output, Error);
    }

    [Fact] void should_report_could_not_run() => _exit.ShouldEqual(2);
    [Fact] void should_name_unreadable_notes() => Error.ToString().Contains("UnreadableNotes", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_ignore_notes_without_the_flag() => _ordinaryExit.ShouldEqual(0);
    [Fact] void should_leave_the_file_unchanged() => File.ReadAllText(NotesPath).ShouldEqual("{");
}
