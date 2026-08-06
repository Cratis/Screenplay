// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileWriter;

public class when_expanding_an_application : Specification
{
    const string Source =
        """
        domain Sales

        concept InvoiceId : Uuid

        module Invoicing
          description "Everything related to invoicing customers"

          layout MasterDetail
            template
              sidebar
              main

          feature Invoices
            description "Registering and managing invoices"

            slice StateChange Register
              event InvoiceRegistered
                invoiceId InvoiceId

            feature Archiving

              slice StateChange Archive
                event InvoiceArchived

        seed
          for "invoices"
            InvoiceRegistered
        """;

    ApplicationSyntax _application;
    IEnumerable<PlayFileContent> _files;

    void Establish() => _application = new ScreenplayCompiler().Compile(Source).Value!;

    void Because() => _files = new PlayFileWriter().Expand(_application);

    [Fact] void should_write_a_file_per_declaration() => _files.Select(file => file.RelativePath).ShouldContainOnly(
    [
        "application.play",
        Path.Combine("Invoicing", "Invoicing.play"),
        Path.Combine("Invoicing", "Invoices", "Invoices.play"),
        Path.Combine("Invoicing", "Invoices", "Archiving", "Archiving.play"),
        Path.Combine("Invoicing", "Invoices", "Archiving", "Archive", "Archive.play"),
        Path.Combine("Invoicing", "Invoices", "Register", "Register.play")
    ]);

    [Fact] void should_put_the_domain_in_the_root_file() => Content("application.play").ShouldContain("domain Sales");
    [Fact] void should_put_the_concepts_in_the_root_file() => Content("application.play").ShouldContain("concept InvoiceId : Uuid");
    [Fact] void should_put_the_seed_in_the_root_file() => Content("application.play").ShouldContain("seed");
    [Fact] void should_keep_the_modules_out_of_the_root_file() => Content("application.play").ShouldNotContain("module Invoicing");
    [Fact] void should_put_the_module_description_in_the_module_file() => Content(Path.Combine("Invoicing", "Invoicing.play")).ShouldContain("description \"Everything related to invoicing customers\"");
    [Fact] void should_put_the_layouts_in_the_module_file() => Content(Path.Combine("Invoicing", "Invoicing.play")).ShouldContain("layout MasterDetail");
    [Fact] void should_keep_the_features_out_of_the_module_file() => Content(Path.Combine("Invoicing", "Invoicing.play")).ShouldNotContain("feature");
    [Fact] void should_put_the_feature_description_in_the_feature_file() => Content(Path.Combine("Invoicing", "Invoices", "Invoices.play")).ShouldContain("description \"Registering and managing invoices\"");
    [Fact] void should_keep_the_slices_out_of_the_feature_file() => Content(Path.Combine("Invoicing", "Invoices", "Invoices.play")).ShouldNotContain("slice");
    [Fact] void should_put_the_slice_in_its_own_file() => Content(Path.Combine("Invoicing", "Invoices", "Register", "Register.play")).ShouldContain("slice StateChange Register");
    [Fact] void should_restate_the_module_a_slice_belongs_to() => Content(Path.Combine("Invoicing", "Invoices", "Register", "Register.play")).ShouldContain("module Invoicing");
    [Fact] void should_restate_the_feature_a_slice_belongs_to() => Content(Path.Combine("Invoicing", "Invoices", "Register", "Register.play")).ShouldContain("feature Invoices");
    [Fact] void should_restate_the_whole_chain_of_a_nested_slice() => Content(Path.Combine("Invoicing", "Invoices", "Archiving", "Archive", "Archive.play")).ShouldContain("feature Archiving");
    [Fact] void should_not_repeat_a_description_below_the_declaration_that_owns_it() => Content(Path.Combine("Invoicing", "Invoices", "Register", "Register.play")).ShouldNotContain("Registering and managing invoices");

    string Content(string relativePath) => _files.Single(file => file.RelativePath == relativePath).Content;
}
