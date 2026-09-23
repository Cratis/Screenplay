// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_merging_behavior_attachments;

public class and_the_module_attachment_is_in_a_deeper_file : when_compiling_a_folder.given.a_folder_of_play_files
{
    ApplicationCompilation<ApplicationSyntax> _compilation;

    void Establish()
    {
        Write(
            Path.Combine("Alpha", "Alpha.play"),
            """
            module Alpha
              description "The module's own file"
            """);

        Write(
            Path.Combine("Alpha", "Ordering", "Ordering.play"),
            """
            module Alpha
              uses NoSuchBehavior

              feature Ordering
                slice StateChange Order
                  event Ordered
            """);
    }

    void Because() => _compilation = _compiler.CompileFolder(_root.FullName);

    [Fact] void should_keep_the_attachment() => _compilation.Result.Value!.Modules.Single().UsedBehaviors.Single().Behavior.ShouldEqual("NoSuchBehavior");
    [Fact] void should_validate_the_attachment() => _compilation.Result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownUsedBehavior);
    [Fact] void should_locate_it_in_the_deeper_file() => _compilation.Result.Diagnostics.Single().Location.Path.ShouldEqual(Path.Combine("Alpha", "Ordering", "Ordering.play"));
}
