// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_a_join_populates_the_field : given.a_view
{
    void Establish() => Compile("""
        slice StateView View
          event Changed
            ownerId Uuid
          event OwnerChanged
            name String
          readmodel R
            ownerId Uuid
            name String
          projection P => R
            from Changed
            join owner on ownerId
              with OwnerChanged
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_accept_join_automap() => Findings.ShouldBeEmpty();
}
