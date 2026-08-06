// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_unknown_identity_path : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid

                produces InvoiceRegistered
                  registeredBy         = $context.identity.id
                  registeredDepartment = $context.identity.claims.department
                  registeredRank       = $context.identity.employeeNumber

              event InvoiceRegistered
                registeredBy         String
                registeredDepartment String
                registeredRank       String
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_only_about_the_unknown_property() => _result.Diagnostics.Count().ShouldEqual(1);
    [Fact] void should_say_what_the_identity_carries() => _result.Diagnostics.Single().Message.ShouldEqual("Unknown $context.identity property 'employeeNumber' - expected id, name, userName, isAuthenticated, roles, claims");
    [Fact] void should_report_a_warning() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
