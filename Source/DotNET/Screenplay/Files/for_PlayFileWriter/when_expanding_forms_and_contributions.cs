// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileWriter;

public class when_expanding_forms_and_contributions : Specification
{
    const string Source =
        """
        behavior Confirming
          on click
            execute Register

        module Sales
          uses Confirming

          screen template Shell
            navbar contributes Navigation
            main

          form RegisterForm for Register
            field name

          contribute to Navigation
            navigate to List
            label "Sales"

          feature Orders
            contribute to Navigation
              navigate to List
              label "Orders"

            on enter
              navigate to List

            slice StateChange Register
              command Register
                name String

            feature Browsing
              contribute to Navigation
                navigate to List
                label "Browse"

              slice StateView List
                screen List
        """;

    IReadOnlyList<PlayFileContent> _files;
    CompilationResult<ApplicationSyntax> _merged;
    IReadOnlyList<PlayFileContent> _expandedAgain;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        var application = compiler.Compile(Source);
        application.Diagnostics.ShouldBeEmpty();
        var writer = new PlayFileWriter();
        _files = [.. writer.Expand(application.Value!)];
        var files = Substitute.For<IPlayFiles>();
        var paths = _files.OrderBy(file => file.RelativePath, StringComparer.Ordinal).Select(file => new PlayFile(file.RelativePath, file.RelativePath)).ToArray();
        files.FindIn("model").Returns(paths);
        foreach (var file in _files)
        {
            files.ReadContent(Arg.Is<PlayFile>(path => path.RelativePath == file.RelativePath)).Returns(file.Content);
        }

        _merged = new PlayFileCompiler(files, compiler).CompileFolder("model").Result;
        _expandedAgain = [.. writer.Expand(_merged.Value!)];
    }

    [Fact] void should_write_six_documents() => _files.Count.ShouldEqual(6);
    [Fact] void should_write_the_form_only_in_the_module_document() => _files.Where(file => file.Content.Contains("form RegisterForm", StringComparison.Ordinal)).Select(file => file.RelativePath).ShouldContainOnly(Path.Combine("Sales", "Sales.play"));
    [Fact] void should_write_the_module_contribution_only_in_the_module_document() => _files.Where(file => file.Content.Contains("label \"Sales\"", StringComparison.Ordinal)).Select(file => file.RelativePath).ShouldContainOnly(Path.Combine("Sales", "Sales.play"));
    [Fact] void should_write_the_feature_contribution_only_in_its_document() => _files.Where(file => file.Content.Contains("label \"Orders\"", StringComparison.Ordinal)).Select(file => file.RelativePath).ShouldContainOnly(Path.Combine("Sales", "Orders", "Orders.play"));
    [Fact] void should_write_the_nested_contribution_only_in_its_document() => _files.Where(file => file.Content.Contains("label \"Browse\"", StringComparison.Ordinal)).Select(file => file.RelativePath).ShouldContainOnly(Path.Combine("Sales", "Orders", "Browsing", "Browsing.play"));
    [Fact] void should_merge_without_diagnostics() => _merged.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_one_form() => _merged.Value!.Modules.Single().Forms!.Count().ShouldEqual(1);
    [Fact] void should_preserve_one_module_contribution() => _merged.Value!.Modules.Single().Contributions!.Count().ShouldEqual(1);
    [Fact] void should_preserve_one_feature_contribution() => _merged.Value!.Modules.Single().Features.Single().Contributions!.Count().ShouldEqual(1);
    [Fact] void should_preserve_one_nested_contribution() => _merged.Value!.Modules.Single().Features.Single().Features.Single().Contributions!.Count().ShouldEqual(1);
    [Fact] void should_write_the_behavior_only_in_the_application_document() => _files.Where(file => file.Content.Contains("behavior Confirming", StringComparison.Ordinal)).Select(file => file.RelativePath).ShouldContainOnly("application.play");
    [Fact] void should_write_the_module_attachment_only_in_the_module_document() => _files.Where(file => file.Content.Contains("uses Confirming", StringComparison.Ordinal)).Select(file => file.RelativePath).ShouldContainOnly(Path.Combine("Sales", "Sales.play"));
    [Fact] void should_write_the_feature_attachment_only_in_its_document() => _files.Where(file => file.Content.Contains("on enter", StringComparison.Ordinal)).Select(file => file.RelativePath).ShouldContainOnly(Path.Combine("Sales", "Orders", "Orders.play"));
    [Fact] void should_preserve_one_behavior() => _merged.Value!.Behaviors.Count().ShouldEqual(1);
    [Fact] void should_preserve_one_module_attachment() => _merged.Value!.Modules.Single().UsedBehaviors.Count().ShouldEqual(1);
    [Fact] void should_preserve_one_feature_attachment() => _merged.Value!.Modules.Single().Features.Single().Behaviors.Count().ShouldEqual(1);
    [Fact] void should_expand_identically_after_merging() => _expandedAgain.ShouldContainOnly(_files);
}
