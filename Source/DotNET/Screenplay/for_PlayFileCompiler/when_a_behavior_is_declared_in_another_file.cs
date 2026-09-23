// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.for_PlayFileCompiler;

/// <summary>
/// A folder compiles to one application, so which file a declaration sits in carries no meaning. A behavior
/// declared in one file has to be attachable from another - if the merge drops it, every multi-file document
/// gets told its own behavior does not exist.
/// </summary>
public class when_a_behavior_is_declared_in_another_file : given.a_play_file_compiler
{
    const string BehaviorSource =
        """
        behavior ConfirmThenExecute
          parameter command
          parameter message

          on click
            confirm message
              on success
                execute command
        """;

    const string ScreenSource =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid

              screen RegisterInvoice
                uses ConfirmThenExecute
                  command RegisterInvoice
                  message $strings.confirmRegister
        """;

    ApplicationCompilation<Syntax.ApplicationSyntax> _compilation;

    void Establish()
    {
        var behaviors = new PlayFile(Path.Combine("root", "behaviors.play"), "behaviors.play");
        var screens = new PlayFile(Path.Combine("root", "screens.play"), "screens.play");
        _playFiles.FindIn("root").Returns([behaviors, screens]);
        _playFiles.ReadContent(behaviors).Returns(BehaviorSource);
        _playFiles.ReadContent(screens).Returns(ScreenSource);
    }

    void Because() => _compilation = _compiler.CompileFolder("root");

    [Fact] void should_compile_the_folder() => _compilation.Result.Success.ShouldBeTrue();
    [Fact] void should_resolve_the_behavior_across_the_files() => _compilation.Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_carry_the_behavior_into_the_merged_application() => _compilation.Result.Value!.Behaviors.Single().Name.ShouldEqual("ConfirmThenExecute");
}
