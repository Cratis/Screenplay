// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_variants_have_different_coverage : given.a_view
{
    void Establish() => Compile("""
        slice StateView View
          event Opened
            name String
          event Closed
          readmodel Active
            name String
          readmodel Inactive
            name String
          projection P
            variant Active
              enters on Opened
              from Opened
            variant Inactive
              enters on Closed
              from Closed
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_not_share_a_sibling_variants_origin() => Findings.Single().Message.ShouldContain("'Inactive' field 'name'");
}
