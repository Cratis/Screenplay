// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_a_performer_serves_the_view : given.a_view
{
    void Establish() => Compile("""
        slice StateView View
          readmodel R
            name String
          query Q => R[]
            performer
              ```csharp
              return [];
              ```
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_accept_the_performer_origin() => Findings.ShouldBeEmpty();
}
