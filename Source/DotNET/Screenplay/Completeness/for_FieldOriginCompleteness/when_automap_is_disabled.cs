// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_automap_is_disabled : given.a_view
{
    void Establish() => Compile("""
        slice StateView View
          event Changed
            name String
          readmodel R
            name String
          projection P => R
            no automap
            from Changed
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_not_count_same_named_event_fields() => Findings.Single().Message.ShouldContain("field 'name'");
}
