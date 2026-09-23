// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_merging_behavior_attachments;

public class and_an_identical_inline_behavior_is_attached_in_two_files : when_compiling_a_folder.given.a_folder_of_play_files
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

              on enter
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

    [Fact] void should_keep_the_attachment_once() => _module.Behaviors.Single().Location.Path.ShouldEqual(Path.Combine("Alpha", "Alpha.play"));
    [Fact] void should_report_the_repeated_attachment() => _compilation.Result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateBehaviorAttachment);
    [Fact] void should_report_it_as_a_warning() => _compilation.Result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_locate_the_later_attachment() => _compilation.Result.Diagnostics.Single().Location.Path.ShouldEqual(Path.Combine("Alpha", "Browsing", "List", "List.play"));
    [Fact] void should_name_the_first_file() => _compilation.Result.Diagnostics.Single().Message.ShouldEqual($"An identical inline behavior is already attached to the module 'Alpha' in '{Path.Combine("Alpha", "Alpha.play")}' - this repeated attachment is ignored");
}
