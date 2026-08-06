// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder;

public class and_a_visitor_is_given : given.a_folder_of_play_files
{
    ApplicationCompilation<string> _compilation;

    void Establish()
    {
        Write(
            Path.Combine("Invoicing", "Invoices", "Register", "Register.play"),
            """
            module Invoicing
              feature Invoices
                slice StateChange Register
                  event InvoiceRegistered
            """);

        Write(
            Path.Combine("Invoicing", "Invoices", "Submit", "Submit.play"),
            """
            module Invoicing
              feature Invoices
                slice StateChange Submit
                  command Submit
                    produces InvoiceRegistered
            """);
    }

    void Because() => _compilation = _compiler.CompileFolder(_root.FullName, new slice_names());

    [Fact] void should_succeed() => _compilation.Result.Success.ShouldBeTrue();
    [Fact] void should_drive_the_visitor_over_the_merged_application() => _compilation.Result.Value.ShouldEqual("Invoicing/Invoices: Register, Submit");
    [Fact] void should_carry_every_source() => _compilation.Sources.Count().ShouldEqual(2);

    class slice_names : IApplicationSyntaxVisitor<string>
    {
        public string Visit(ApplicationSyntax syntax) =>
            string.Join(
                "; ",
                syntax.Modules.SelectMany(module => module.Features.Select(feature =>
                    $"{module.Name}/{feature.Name}: {string.Join(", ", feature.Slices.Select(slice => slice.Name))}")));
    }
}
