// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_names_that_collide_with_directives : given.a_printer
{
    const string Source =
        """
        concept InvoiceStatus : Enum
          draft
          @validate

        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId   Uuid
                description String
                @authorize  AuthorizationCode
                @produces   ProductionLine

                produces InvoiceRegistered
                  invoiceId = invoiceId
                  @tag      = invoiceId

              event InvoiceRegistered
                @tag      TagType
                invoiceId Uuid
        """;

    given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_escape_the_enum_value() => _roundtrip.Printed.ShouldContain("@validate");
    [Fact] void should_escape_the_authorize_property() => _roundtrip.Printed.ShouldContain("@authorize AuthorizationCode");
    [Fact] void should_escape_the_produces_property() => _roundtrip.Printed.ShouldContain("@produces ProductionLine");
    [Fact] void should_escape_the_tag_property() => _roundtrip.Printed.ShouldContain("@tag TagType");
    [Fact] void should_escape_the_tag_mapping() => _roundtrip.Printed.ShouldContain("@tag = invoiceId");
    [Fact] void should_leave_the_description_property_unescaped() => _roundtrip.Printed.ShouldContain("description String");
    [Fact] void should_preserve_the_command_properties() => Command(_roundtrip.Reparsed).Properties.Count().ShouldEqual(4);
    [Fact] void should_preserve_the_authorize_property() => Command(_roundtrip.Reparsed).Properties.Any(_ => _.Name == "authorize").ShouldBeTrue();
    [Fact] void should_preserve_the_event_tag_property() => Event(_roundtrip.Reparsed).Properties.Any(_ => _.Name == "tag").ShouldBeTrue();
    [Fact] void should_preserve_the_enum_value() => _roundtrip.Reparsed.Value!.Concepts.Single().Values.ShouldContain("validate");

    static CommandSyntax Command(CompilationResult<ApplicationSyntax> result) =>
        result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single();

    static EventSyntax Event(CompilationResult<ApplicationSyntax> result) =>
        result.Value!.Modules.Single().Features.Single().Slices.Single().Events.Single();
}
