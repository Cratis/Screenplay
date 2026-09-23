// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_merging_behavior_attachments;

public class and_the_feature_attachment_is_in_a_slice_file : when_compiling_a_folder.given.a_folder_of_play_files
{
    ApplicationCompilation<ApplicationSyntax> _compilation;

    void Establish()
    {
        Write(
            Path.Combine("Alpha", "Ordering", "Ordering.play"),
            """
            module Alpha
              feature Ordering
                description "The feature's own file"
            """);

        Write(
            Path.Combine("Alpha", "Ordering", "Order", "Order.play"),
            """
            module Alpha
              feature Ordering
                uses NoSuchBehavior

                slice StateChange Order
                  event Ordered
            """);
    }

    void Because() => _compilation = _compiler.CompileFolder(_root.FullName);

    [Fact] void should_keep_the_attachment() => _compilation.Result.Value!.Modules.Single().Features.Single().UsedBehaviors.Single().Behavior.ShouldEqual("NoSuchBehavior");
    [Fact] void should_validate_the_attachment() => _compilation.Result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownUsedBehavior);
    [Fact] void should_locate_it_in_the_slice_file() => _compilation.Result.Diagnostics.Single().Location.Path.ShouldEqual(Path.Combine("Alpha", "Ordering", "Order", "Order.play"));
}
