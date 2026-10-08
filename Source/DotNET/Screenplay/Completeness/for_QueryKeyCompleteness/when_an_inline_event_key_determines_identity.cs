// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_an_inline_event_key_determines_identity : given.a_query
{
    void Establish()
    {
        const string source = "module M\n  feature F\n    slice StateView View\n      event Changed\n        sourceId Uuid\n      readmodel R\n        id Uuid\n      projection P => R\n        from Changed key sourceId\n          id = sourceId\n      query Get => R optional\n        by missing Int";
        Compilation = new ScreenplayCompiler().Compile(source);
        Compilation.Diagnostics.ShouldBeEmpty();
    }

    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_use_the_inline_key_source_type() => Findings.Length.ShouldEqual(1);
}
