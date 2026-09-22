// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_resolving_consistency_declarations_in_scope : given.a_consistent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source + "\n" +
        """
        module Other
          feature Entries
            slice StateChange Record
              command RecordEntry
                text String
                kind RowKind
                produces EntryRecorded
                  comment = text
                  kind = kind
              event EntryRecorded
                comment String
                kind RowKind
              specification RecordsOtherEntry
                when RecordEntry
                  text = "other"
                  kind = added
                then EntryRecorded
                  comment = "other"
                  kind = "added"
            slice StateView Browse
              projection OtherEntries => OtherView
                from EntryRecorded
              query EntryByObservation => OtherView
                by scope ScopeId
        """);

    [Fact] void should_resolve_each_command_and_event_in_its_own_slice() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_mix_same_named_query_signatures_or_enum_fields() => _result.Diagnostics.ShouldBeEmpty();
}
