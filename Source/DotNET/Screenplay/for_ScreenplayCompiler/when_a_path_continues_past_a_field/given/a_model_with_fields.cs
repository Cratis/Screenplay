// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_a_path_continues_past_a_field.given;

public class a_model_with_fields : for_ScreenplayCompiler.given.a_compiler
{
    protected static string Model(string assigned, string validated = "note", string field = "", string import = "") =>
        $"""
        {import}
        concept Title : String
        concept Kind : Enum
          added
        type Detail
          note String
        module Records
          feature Entries
            slice StateChange Record
              command RecordEntry
                title Title
                note String
                validate
                  {validated} not empty
                produces EntryRecorded
                  {assigned} = note
              event EntryRecorded
                title Title
                note String
                kind Kind
                detail Detail
                {field}
        """;
}
