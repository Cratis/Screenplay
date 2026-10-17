// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_imports_are_queried_by_config : given.a_query
{
    void Establish()
    {
        const string source = """
            concept ImportId : Uuid
            concept ConfigId : Uuid
            module Imports
              feature Importing
                slice StateView Imports
                  event ImportStarted
                    importId ImportId
                    configId ConfigId
                  readmodel ImportView
                    importId ImportId
                    configId ConfigId
                  projection Imports => ImportView
                    from ImportStarted
                      key importId
                  query GetImport => ImportView optional
                    by importId ImportId
                  query ImportsForConfig => ImportView[]
                    by configId ConfigId
            """;
        Compilation = new ScreenplayCompiler().Compile(source);
        Compilation.Diagnostics.ShouldBeEmpty();
        Compilation.Success.ShouldBeTrue();
    }

    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_accept_both_the_identity_and_collection_field_parameters() => Findings.ShouldBeEmpty();
}
