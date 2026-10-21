// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_merging_template_assignments;

// The same holds for a feature: its `template` assignment survives when the file that says it is not the first to restate the feature, and the same reference
// restated in a second file is one assignment, not two.
public class and_the_feature_assignment_is_in_a_later_file : when_compiling_a_folder.given.a_folder_of_play_files
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
            Path.Combine("Alpha", "Feature.play"),
            """
            module Alpha
              feature Ordering
                description "The feature's own file"
                template blueprint Cratis.Blueprint.Default ListScreen version "^1"
            """);

        Write(
            Path.Combine("Alpha", "Zebra.play"),
            """
            module Alpha
              feature Ordering
                template blueprint Cratis.Blueprint.Default ListScreen version "^1"

                slice StateChange Cancel
                  event Cancelled
            """);
    }

    void Because() => _compilation = _compiler.CompileFolder(_root.FullName);

    [Fact] void should_keep_one_assignment() => _compilation.Result.Value!.Modules.Single().Features.Single().Templates.Count().ShouldEqual(1);
    [Fact] void should_keep_the_assigned_template() => _compilation.Result.Value!.Modules.Single().Features.Single().Templates.Single().Name.ShouldEqual("ListScreen");
}
