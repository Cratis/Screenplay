// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_merging_behavior_attachments;

// The shape a folder written by 4.17.0 has: expansion restated a module's and a feature's attachments in every
// file below it.
public class and_every_file_restates_the_attachments : when_compiling_a_folder.given.a_folder_of_play_files
{
    ApplicationCompilation<ApplicationSyntax> _compilation;
    ModuleSyntax _module;
    FeatureSyntax _feature;

    void Establish()
    {
        Write(
            "application.play",
            """
            behavior Refreshing
              on enter
                navigate to List
            """);

        Write(
            Path.Combine("Alpha", "Alpha.play"),
            """
            module Alpha
              uses Refreshing

              on click
                navigate to List
            """);

        Write(
            Path.Combine("Alpha", "Browsing", "Browsing.play"),
            """
            module Alpha
              uses Refreshing

              on click
                navigate to List

              feature Browsing
                on enter
                  navigate to List
            """);

        Write(
            Path.Combine("Alpha", "Browsing", "List", "List.play"),
            """
            module Alpha
              uses Refreshing

              on click
                navigate to List

              feature Browsing
                on enter
                  navigate to List

                slice StateView List
                  screen List
            """);
    }

    void Because()
    {
        _compilation = _compiler.CompileFolder(_root.FullName);
        _module = _compilation.Result.Value!.Modules.Single();
        _feature = _module.Features.Single();
    }

    [Fact] void should_keep_one_module_attachment() => _module.UsedBehaviors.Count().ShouldEqual(1);
    [Fact] void should_keep_one_inline_module_behavior() => _module.Behaviors.Count().ShouldEqual(1);
    [Fact] void should_keep_one_inline_feature_behavior() => _feature.Behaviors.Count().ShouldEqual(1);
    [Fact] void should_report_every_repeated_copy() => _compilation.Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(Enumerable.Repeat(DiagnosticCodes.DuplicateBehaviorAttachment, 5));
    [Fact] void should_still_succeed() => _compilation.Result.Success.ShouldBeTrue();
}
