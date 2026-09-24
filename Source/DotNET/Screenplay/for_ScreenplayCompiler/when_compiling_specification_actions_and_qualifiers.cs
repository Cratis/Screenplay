// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_specification_actions_and_qualifiers : given.a_compiler
{
    CompilationResult<SpecificationSyntax> _mixed;
    CompilationResult<SpecificationSyntax> _badOrder;
    CompilationResult<SpecificationSyntax> _defaults;

    void Because()
    {
        _mixed = _compiler.CompileSpecification(
            """
            specification Mixed
              when RegisterInvoice
              when append InvoiceRegistered
              then InvoiceRegistered
            """);
        _badOrder = _compiler.CompileSpecification(
            """
            specification BadOrder
              when append InvoiceRegistered
              then events in reverse order
              then InvoiceRegistered
            """);
        _defaults = _compiler.CompileSpecification(
            """
            specification Defaults
              when RegisterInvoice
              then InvoiceRegistered
              then readmodel InvoiceSummary
              then query InvoiceById
            """);
    }

    [Fact] void should_reject_two_actions() => _mixed.Diagnostics.Any(value => value.Code == DiagnosticCodes.ConflictingSpecificationActions).ShouldBeTrue();
    [Fact] void should_reject_an_unknown_order_qualifier() => _badOrder.Diagnostics.Any(value => value.Code == DiagnosticCodes.InvalidSpecificationEventOrder).ShouldBeTrue();
    [Fact] void should_keep_event_order_as_the_default() => _defaults.Value!.ThenEventsInAnyOrder.ShouldBeFalse();
    [Fact] void should_keep_read_model_subset_as_the_default() => _defaults.Value!.ThenReadModels!.Single().Exactly.ShouldBeFalse();
    [Fact] void should_keep_query_subset_as_the_default() => _defaults.Value!.ThenQueries.Single().Exactly.ShouldBeFalse();
}
