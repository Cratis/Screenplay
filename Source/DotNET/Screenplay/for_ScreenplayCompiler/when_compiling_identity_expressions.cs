// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_identity_expressions : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(
        """
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                produces InvoiceRegistered
                  userName = $identity.userName
                  claim = $identity.claims.anything.here
                  unknown = $identity.nope
                  legacy = $context.identity.id
              event InvoiceRegistered
                userName String
                claim String
                unknown String
                legacy String
        """);

    ExpressionSyntax Source(string property) => _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single().Mappings.Single(mapping => mapping.Property == property).Source;

    [Fact] void should_parse_the_identity_root() => Source("userName").ShouldBeOfExactType<IdentityExpressionSyntax>();
    [Fact] void should_preserve_the_path() => ((IdentityExpressionSyntax)Source("userName")).Path.ShouldEqual("userName");
    [Fact] void should_keep_context_as_its_existing_node() => Source("legacy").ShouldBeOfExactType<ContextExpressionSyntax>();
    [Fact] void should_keep_the_legacy_path() => ((ContextExpressionSyntax)Source("legacy")).Path.ShouldEqual("identity.id");
    [Fact] void should_keep_the_opaque_claim_path() => ((IdentityExpressionSyntax)Source("claim")).Path.ShouldEqual("claims.anything.here");
    [Fact] void should_warn_only_for_the_unknown_property() => _result.Diagnostics.Count().ShouldEqual(1);
    [Fact] void should_report_a_warning() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_reuse_the_identity_warning_code() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownContextIdentityProperty);
    [Fact] void should_name_the_identity_root() => _result.Diagnostics.Single().Message.ShouldEqual("Unknown $identity property 'nope' - expected id, name, userName, isAuthenticated, roles, claims");
}
