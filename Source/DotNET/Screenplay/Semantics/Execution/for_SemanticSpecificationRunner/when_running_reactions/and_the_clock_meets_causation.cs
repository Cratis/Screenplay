// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_the_clock_meets_causation : given.a_v6_scenario
{
    const string Source =
        """
        concept NoteId : Uuid
        module Notes
          feature Writing
            slice StateChange WriteNote
              command WriteNote
                noteId NoteId identifier
                text String
                produces NoteWritten
                  for noteId
                  text = text
                  writtenAt = $context.occurred
                  writtenBy = $context.causedBy.userName
              event NoteWritten
                text String
                writtenAt DateTime
                writtenBy String
              specification WritingANote
                given clock "2026-10-02T09:00:00Z"
                when WriteNote
                  noteId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                  text = "Hello"
                then NoteWritten
                  text = "Hello"
                  writtenAt = "2026-10-02T09:00:00Z"
                  writtenBy = "ada"
        """;

    SemanticSpecificationRun _run;

    void Establish() => Compile(Source);

    void Because() => _run = Run("WritingANote");

    [Fact] void should_not_invent_who_caused_it() => ((SemanticUnsupported)_run.Execution).Details.ShouldContain("$context.causedBy");
}
