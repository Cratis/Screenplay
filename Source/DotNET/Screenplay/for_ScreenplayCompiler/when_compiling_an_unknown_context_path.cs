// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_unknown_context_path : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid

                produces InvoiceRegistered
                  registeredAt   = $context.occurred
                  registeredFor  = $context.tenant
                  registeredBy   = $context.causedBy.subject
                  registeredWhen = $context.wallClock
                  registeredWho  = $context.causedBy.employeeNumber

              event InvoiceRegistered
                registeredAt   DateTime
                registeredFor  Uuid
                registeredBy   String
                registeredWhen DateTime
                registeredWho  String
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_once_per_unknown_path() => _result.Diagnostics.Count().ShouldEqual(2);
    [Fact] void should_warn_about_the_unknown_root() => _result.Diagnostics.First().Message.ShouldEqual("Unknown $context path 'wallClock' - expected one of command, arguments, tenant, causedBy, causation, occurred, identity");
    [Fact] void should_warn_about_the_unknown_caused_by_property() => _result.Diagnostics.Last().Message.ShouldEqual("Unknown $context.causedBy property 'employeeNumber' - expected subject, name, userName");
    [Fact] void should_report_warnings_only() => _result.Diagnostics.All(_ => _.Severity == DiagnosticSeverity.Warning).ShouldBeTrue();
}
