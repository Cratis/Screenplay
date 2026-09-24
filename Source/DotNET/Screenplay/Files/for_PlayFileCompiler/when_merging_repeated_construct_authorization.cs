// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler;

public class when_merging_repeated_construct_authorization : when_compiling_a_folder.given.a_folder_of_play_files
{
    ApplicationCompilation<ApplicationSyntax> _compilation;

    void Establish()
    {
        Write("application.play", """
            policy Staff
              require role "Staff"
            policy Finance
              require role "Finance"
            """);
        Write(Path.Combine("Portal", "Reports", "Reports.play"), """
            module Portal
              feature Reports
                slice StateChange FileReport
                  command FileReport
                    authorize Staff
                    authorize Finance
                  readmodel Report
                    id Uuid
                  query ReportById => Report?
                    by id Uuid
                    authorize Staff
                    authorize Finance
            """);
    }

    void Because() => _compilation = _compiler.CompileFolder(_root.FullName);

    [Fact] void should_compile_without_diagnostics() => _compilation.Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_both_command_gates() => _compilation.Result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Authorize!.References().Select(reference => reference.Name).ShouldContainOnly("Staff", "Finance");
    [Fact] void should_keep_both_query_gates() => _compilation.Result.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single().Authorize!.References().Select(reference => reference.Name).ShouldContainOnly("Staff", "Finance");
}
