// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder;

public class and_declarations_are_spread_across_files : given.a_folder_of_play_files
{
    ApplicationCompilation<ApplicationSyntax> _compilation;
    FeatureSyntax _feature;

    void Establish()
    {
        Write(
            "application.play",
            """
            concept InvoiceId : Uuid

            policy CanManageInvoice
              require role "accountant"
            """);

        Write(
            Path.Combine("Invoicing", "Invoices", "Register", "Register.play"),
            """
            module Invoicing
              feature Invoices
                slice StateChange Register
                  event InvoiceRegistered
                    invoiceId InvoiceId
            """);

        Write(
            Path.Combine("Invoicing", "Invoices", "Submit", "Submit.play"),
            """
            module Invoicing
              feature Invoices
                slice StateChange Submit
                  command Submit
                    invoiceId InvoiceId
                    authorize CanManageInvoice
                    produces InvoiceRegistered
            """);
    }

    void Because()
    {
        _compilation = _compiler.CompileFolder(_root.FullName);
        _feature = _compilation.Result.Value!.Modules.Single().Features.Single();
    }

    [Fact] void should_succeed() => _compilation.Result.Success.ShouldBeTrue();
    [Fact] void should_resolve_every_cross_file_reference() => _compilation.Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_carry_every_source() => _compilation.Sources.Count().ShouldEqual(3);
    [Fact] void should_merge_the_files_into_one_module() => _compilation.Result.Value!.Modules.Count().ShouldEqual(1);
    [Fact] void should_merge_the_files_into_one_feature() => _feature.Name.ShouldEqual("Invoices");
    [Fact] void should_keep_both_slices() => _feature.Slices.Select(slice => slice.Name).ShouldContainOnly(["Register", "Submit"]);
    [Fact] void should_keep_the_concept() => _compilation.Result.Value!.Concepts.Single().Name.ShouldEqual("InvoiceId");
    [Fact] void should_keep_the_policy() => _compilation.Result.Value!.Policies.Single().Name.ShouldEqual("CanManageInvoice");
}
