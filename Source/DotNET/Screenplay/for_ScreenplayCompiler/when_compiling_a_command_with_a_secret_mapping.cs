// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_command_with_a_secret_mapping : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid

                produces InvoiceRegistered
                  invoiceId = invoiceId
                  apiKey    = $secrets.legacyApiKey

              event InvoiceRegistered
                invoiceId Uuid
                apiKey    String
        """;

    CompilationResult<ApplicationSyntax> _result;
    PropertyMappingSyntax _mapping;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _mapping = _result.Value!.Modules.Single().Features.Single().Slices.Single()
            .Commands.Single().Produces.Single().Mappings.Last();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_secret_symbolic() => _mapping.Source.ShouldBeOfExactType<SecretExpressionSyntax>();
    [Fact] void should_parse_the_secret_name() => ((SecretExpressionSyntax)_mapping.Source).Name.ShouldEqual("legacyApiKey");
}
