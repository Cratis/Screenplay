// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder;

public class and_a_module_is_split_across_files : given.a_folder_of_play_files
{
    ApplicationCompilation<ApplicationSyntax> _compilation;
    ModuleSyntax _module;
    FeatureSyntax _feature;

    void Establish()
    {
        Write(
            Path.Combine("Invoicing", "Invoicing.play"),
            """
            module Invoicing
              description "Everything related to invoicing customers"

              screen template MasterDetail
                sidebar
                main
            """);

        Write(
            Path.Combine("Invoicing", "Invoices", "Invoices.play"),
            """
            module Invoicing
              feature Invoices
                description "Registering and managing invoices"
            """);

        Write(
            Path.Combine("Invoicing", "Invoices", "Register", "Register.play"),
            """
            module Invoicing
              feature Invoices
                slice StateChange Register
                  event InvoiceRegistered
            """);
    }

    void Because()
    {
        _compilation = _compiler.CompileFolder(_root.FullName);
        _module = _compilation.Result.Value!.Modules.Single();
        _feature = _module.Features.Single();
    }

    [Fact] void should_succeed() => _compilation.Result.Success.ShouldBeTrue();
    [Fact] void should_not_report_anything() => _compilation.Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_module_description() => _module.Description.ShouldEqual("Everything related to invoicing customers");
    [Fact] void should_keep_the_module_screen_template() => _module.ScreenTemplates.Single().Name.ShouldEqual("MasterDetail");
    [Fact] void should_keep_the_feature_description() => _feature.Description.ShouldEqual("Registering and managing invoices");
    [Fact] void should_keep_the_slice() => _feature.Slices.Single().Name.ShouldEqual("Register");
}
