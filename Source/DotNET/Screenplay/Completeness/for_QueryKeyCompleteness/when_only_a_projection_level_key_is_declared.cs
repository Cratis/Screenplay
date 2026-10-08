// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_only_a_projection_level_key_is_declared : given.a_query
{
    void Establish()
    {
        Compilation = new ScreenplayCompiler().Compile("""
            module M
              feature F
                slice StateView View
                  event Changed
                    sourceId Uuid
                  readmodel R
                    id Uuid
                  projection P => R
                    key sourceId
                    from Changed
                      id = sourceId
                  query Get => R optional
                    by missing Int
            """);
        Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        Compilation.Success.ShouldBeTrue();
    }
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_skip_the_unknown_event_source_identity_type() => Findings.ShouldBeEmpty();
}
