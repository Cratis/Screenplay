// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder;

public class and_reducer_identity_depends_on_other_files : given.a_folder_of_play_files
{
    Diagnostic[] _warnings;

    void Establish()
    {
        Write("Concepts.play", "concept ResourceId : String\nconcept Month : String");
        Write("View.play", """
            module Work
              feature Reporting
                slice StateView Totals
                  event Recorded
                  readmodel Totals
                    month Month key
                  reducer Fold => Totals
                    on Recorded
                  query ByMonth => Totals
                    by month Month
            """);
        Write("Producer.play", """
            module Work
              feature Reporting
                slice StateChange Record
                  command Record
                    resource ResourceId identifier
                    produces Recorded
                      for resource
            """);
    }

    void Because() => _warnings = [.. _compiler.CompileFolder(_root.FullName).Result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.ReducerIdentityMismatch)];

    [Fact] void should_compare_the_assembled_application() => _warnings.Length.ShouldEqual(1);
    [Fact] void should_preserve_the_query_document_location() => _warnings[0].Location.Path!.EndsWith("View.play", StringComparison.Ordinal).ShouldBeTrue();
}
