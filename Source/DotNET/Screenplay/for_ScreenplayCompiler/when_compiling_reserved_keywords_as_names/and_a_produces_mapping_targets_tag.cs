// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_reserved_keywords_as_names;

public class and_a_produces_mapping_targets_tag : given.a_compiler
{
    const string Source =
        """
        concept TagType : String

        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid
                @tag      TagType

                produces InvoiceRegistered
                  tag       audit
                  invoiceId = invoiceId
                  @tag      = tag

              event InvoiceRegistered
                @tag      TagType
                invoiceId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;
    ProducesSyntax _produces;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _produces = _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_static_tag() => _produces.Tags!.Single().ShouldNotBeNull();
    [Fact] void should_map_both_properties() => _produces.Mappings.Count().ShouldEqual(2);
    [Fact] void should_strip_the_escape_from_the_mapping_target() => _produces.Mappings.Single(_ => _.Property == "tag").ShouldNotBeNull();
}
