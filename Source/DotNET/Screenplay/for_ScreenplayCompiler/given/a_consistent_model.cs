// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.given;

public class a_consistent_model : a_compiler
{
    protected const string Source =
        """
        concept ObservationId : Uuid
        concept ScopeId : Uuid
        concept EntryKind : Enum
          created
          updated
        concept RowKind : Enum
          added
        type Row
          rowId Uuid
          note String
          kind RowKind
          audit String
        type Detail
          note String
          code String
        module Recording
          feature Entries
            slice StateChange Record
              command RecordEntry
                startReadingId ObservationId
                note String
                kind EntryKind
                validate
                  note not empty
                  note rule HasMeaning
                    file rules/HasMeaning.cs
                reads EntryView by startReadingId
                produces EntryRecorded
                  note = note
                  kind = kind
              event EntryRecorded
                note String
                kind EntryKind
              event RowAdded
                note String
                kind RowKind
                code String
              specification RecordsEntry
                given RowAdded
                  kind = added
                when RecordEntry
                  note = "authored"
                  kind = created
                then EntryRecorded
                  note = "authored"
                  kind = EntryKind.created
            slice StateView Browse
              readmodel EntryView
                rows Row[]
                detail Detail?
              projection Entries => EntryView
                every
                  audit = "recorded"
                children rows identified by rowId
                  from RowAdded
                nested detail
                  from RowAdded
              query EntryByObservation => EntryView
                by observationId ObservationId
        """;
}
