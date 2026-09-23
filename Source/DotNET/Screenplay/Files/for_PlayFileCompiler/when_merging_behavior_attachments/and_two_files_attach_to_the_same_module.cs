// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_merging_behavior_attachments;

public class and_two_files_attach_to_the_same_module : when_compiling_a_folder.given.a_folder_of_play_files
{
    ApplicationCompilation<ApplicationSyntax> _compilation;
    ModuleSyntax _module;

    void Establish()
    {
        Write(
            "application.play",
            """
            behavior Refreshing
              on enter
                navigate to List

            behavior Opening
              parameter screen
              on click
                navigate to screen
            """);

        Write(
            Path.Combine("Alpha", "Alpha.play"),
            """
            module Alpha
              uses Refreshing
            """);

        Write(
            Path.Combine("Alpha", "Browsing", "List", "List.play"),
            """
            module Alpha
              uses Opening
                screen List

              feature Browsing
                slice StateView List
                  screen List
            """);
    }

    void Because()
    {
        _compilation = _compiler.CompileFolder(_root.FullName);
        _module = _compilation.Result.Value!.Modules.Single();
    }

    [Fact] void should_keep_both_attachments_in_path_order() => _module.UsedBehaviors.Select(uses => uses.Behavior).ShouldEqual(["Refreshing", "Opening"]);
    [Fact] void should_not_report_anything() => _compilation.Result.Diagnostics.ShouldBeEmpty();
}
