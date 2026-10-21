// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_merging_template_assignments;

// A module's `template blueprint ...` assignment is one of its own declarations. The folder merge used to keep only what the first file said, so an assignment written in
// the module's own file - or in any file that sorts after the one restating the module first - was silently dropped (the Acme Commerce sample's Purchasing module).
public class and_the_module_assignment_is_in_a_later_file : when_compiling_a_folder.given.a_folder_of_play_files
{
    ApplicationCompilation<ApplicationSyntax> _compilation;

    void Establish()
    {
        Write(
            Path.Combine("Alpha", "Another.play"),
            """
            module Alpha
              feature Ordering
                slice StateChange Order
                  event Ordered
            """);

        Write(
            Path.Combine("Alpha", "Module.play"),
            """
            module Alpha
              description "The module's own file"
              template blueprint Cratis.Blueprint.Default WorkspaceScreen version "^1"
            """);
    }

    void Because() => _compilation = _compiler.CompileFolder(_root.FullName);

    [Fact] void should_keep_the_assignment() => _compilation.Result.Value!.Modules.Single().Templates.Single().Name.ShouldEqual("WorkspaceScreen");
    [Fact] void should_keep_the_blueprint_it_comes_from() => _compilation.Result.Value!.Modules.Single().Templates.Single().Blueprint.ShouldEqual("Cratis.Blueprint.Default");
    [Fact] void should_keep_the_version_range() => _compilation.Result.Value!.Modules.Single().Templates.Single().Version.ShouldEqual("^1");
}
