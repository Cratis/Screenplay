// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.for_Agents;

public class when_enforcing_model_authorization : given.an_agent_model
{
    [Fact]
    void should_warn_about_an_ungated_operation()
    {
        var result = Compiler.Compile(Model.Replace("  authorize Staff\n", string.Empty, StringComparison.Ordinal) + "\n" + Allowlist);
        result.Success.ShouldBeTrue();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UngatedAgentOperation && diagnostic.Severity == DiagnosticSeverity.Warning).ShouldEqual(2);
    }

    [Fact]
    void should_accept_a_feature_gate()
    {
        var source = Model.Replace("  authorize Staff\n", string.Empty, StringComparison.Ordinal).Replace("  feature Invoices", "  feature Invoices\n    authorize Staff", StringComparison.Ordinal);
        Compiler.Compile(source + "\n" + Allowlist).Success.ShouldBeTrue();
    }

    [Fact]
    void should_accept_operation_gates()
    {
        var source = Model.Replace("  authorize Staff\n", string.Empty, StringComparison.Ordinal)
            .Replace("      query All => Invoice[]", "      query All => Invoice[]\n        authorize Staff", StringComparison.Ordinal)
            .Replace("      command Issue", "      command Issue\n        authorize Staff", StringComparison.Ordinal);
        Compiler.Compile(source + "\n" + Allowlist).Success.ShouldBeTrue();
    }

    [Fact]
    void should_not_add_an_authorize_clause()
    {
        var result = Compiler.Compile(Model + "\n" + Allowlist);
        result.Value!.Modules.Single().Features.Single().Slices.First().Queries.Single().Authorize.ShouldBeNull();
    }

    [Fact]
    void should_still_reject_an_unknown_policy()
    {
        var source = Model.Replace("authorize Staff", "authorize Missing", StringComparison.Ordinal) + "\n" + Allowlist;
        var parsed = Compiler.Compile(source);
        parsed.Success.ShouldBeTrue();
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Agents"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", source);
        var result = new SemanticModelBinder().Bind("Agents", parsed.Value!, SemanticDocumentSet.Create([document], catalog));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Message == "Authorization policy 'Missing' is unresolved.");
    }
}
