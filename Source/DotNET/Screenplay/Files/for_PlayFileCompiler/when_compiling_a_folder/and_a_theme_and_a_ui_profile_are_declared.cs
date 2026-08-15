// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder;

/// <summary>
/// A folder carries every top-level declaration the same document would. Themes and ui profiles were the
/// two that did not, and nothing noticed: the merge builds the application positionally and simply never
/// passed them, so both arrived empty however many the folder declared.
/// </summary>
public class and_a_theme_and_a_ui_profile_are_declared : given.a_folder_of_play_files
{
    ApplicationCompilation<ApplicationSyntax> _compilation;

    void Establish()
    {
        Write(
            "application.play",
            """
            theme Nordic
              compatible with Cratis.Components

            ui profile Desktop
              target platform web
              packages
                Cratis.Components
              theme Nordic
            """);

        Write(
            Path.Combine("Invoicing", "Invoices", "Register", "Register.play"),
            """
            module Invoicing
              feature Invoices
                slice StateChange Register
                  event InvoiceRegistered
                    invoiceId Uuid
            """);
    }

    void Because() => _compilation = _compiler.CompileFolder(_root.FullName);

    [Fact] void should_succeed() => _compilation.Result.Success.ShouldBeTrue();
    [Fact] void should_resolve_every_reference() => _compilation.Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_theme() => _compilation.Result.Value!.Themes!.Single().Name.ShouldEqual("Nordic");
    [Fact] void should_keep_what_the_theme_is_compatible_with() => _compilation.Result.Value!.Themes!.Single().CompatibleWith.ShouldContainOnly("Cratis.Components");
    [Fact] void should_keep_the_ui_profile() => _compilation.Result.Value!.UiProfiles!.Single().Name.ShouldEqual("Desktop");
    [Fact] void should_keep_the_theme_the_profile_selects() => _compilation.Result.Value!.UiProfiles!.Single().Theme.ShouldEqual("Nordic");
}
