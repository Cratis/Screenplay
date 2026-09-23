// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_merging_behavior_attachments;

public class and_different_inline_behaviors_are_attached_in_two_files : when_compiling_a_folder.given.a_folder_of_play_files
{
    ApplicationCompilation<ApplicationSyntax> _compilation;
    ModuleSyntax _module;

    void Establish()
    {
        Write(
            Path.Combine("Alpha", "Alpha.play"),
            """
            module Alpha
              on enter
                navigate to List
            """);

        Write(
            Path.Combine("Alpha", "Browsing", "List", "List.play"),
            """
            module Alpha

              on click
                navigate to List

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

    [Fact] void should_keep_both_attachments_in_path_order() => _module.Behaviors.Select(behavior => behavior.Location.Path).ShouldEqual([Path.Combine("Alpha", "Alpha.play"), Path.Combine("Alpha", "Browsing", "List", "List.play")]);
    [Fact] void should_not_report_anything() => _compilation.Result.Diagnostics.ShouldBeEmpty();
}
