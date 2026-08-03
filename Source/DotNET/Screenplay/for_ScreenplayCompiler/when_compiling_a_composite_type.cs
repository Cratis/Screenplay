// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_composite_type : given.a_compiler
{
    const string Source =
        """
        concept ProductName        : String
        concept Money              : Decimal
        concept Quantity           : Int
        concept DiscountPercentage : Decimal

        type InvoiceLine
          description "A single billed line of an invoice"
          lineNumber  Int
          productName ProductName
          quantity    Quantity
          unitPrice   Money
          discount    DiscountPercentage?

        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              event InvoiceRegistered
                lines InvoiceLine[]
        """;

    CompilationResult<ApplicationSyntax> _result;
    TypeSyntax _type;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _type = _result.Value!.Types!.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_name_the_type() => _type.Name.ShouldEqual("InvoiceLine");
    [Fact] void should_have_the_description() => _type.Description.ShouldEqual("A single billed line of an invoice");
    [Fact] void should_declare_every_property() => _type.Properties.Select(_ => _.Name).ShouldContainOnly("lineNumber", "productName", "quantity", "unitPrice", "discount");
    [Fact] void should_keep_the_primitive_property_type() => _type.Properties.First().Type.Name.ShouldEqual("Int");
    [Fact] void should_keep_the_optional_suffix() => _type.Properties.Last().Type.IsOptional.ShouldBeTrue();
    [Fact] void should_resolve_the_collection_reference_from_the_event() => Event.Properties.Single().Type.Name.ShouldEqual("InvoiceLine");
    [Fact] void should_keep_the_collection_suffix() => Event.Properties.Single().Type.IsCollection.ShouldBeTrue();

    EventSyntax Event => _result.Value!.Modules.Single().Features.Single().Slices.Single().Events.Single();
}
