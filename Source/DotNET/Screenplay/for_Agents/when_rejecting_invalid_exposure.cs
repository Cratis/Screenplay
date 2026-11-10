// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_Agents;

public class when_rejecting_invalid_exposure : given.an_agent_model
{
    [Theory]
    [InlineData("agents extra\n  tokens personal", DiagnosticCodes.InvalidAgentsDeclaration)]
    [InlineData("agents", DiagnosticCodes.AgentTokensMissing)]
    [InlineData("agents\n  tokens personal\n  tokens personal", DiagnosticCodes.AgentTokensMissing)]
    [InlineData("agents\n  tokens oauth", DiagnosticCodes.UnsupportedAgentTokenKind)]
    [InlineData("agents\n  tokens custom", DiagnosticCodes.UnsupportedAgentTokenKind)]
    [InlineData("agents\n  tokens personal\n  scope write", DiagnosticCodes.GlobalAgentScope)]
    [InlineData("agents\n  tokens personal\n  query All\n    scope read", DiagnosticCodes.AgentOperationMissingDescription)]
    [InlineData("agents\n  tokens personal\n  query All\n    scope read\n    description \" \"", DiagnosticCodes.AgentOperationMissingDescription)]
    [InlineData("agents\n  tokens personal\n  query All\n    description \"Find invoices\"", DiagnosticCodes.AgentOperationMissingScope)]
    [InlineData("agents\n  tokens personal\n  query All\n    scope maybe\n    description \"Find invoices\"", DiagnosticCodes.AgentOperationMissingScope)]
    [InlineData("agents\n  tokens personal\n  query All\n    scope read\n    scope write\n    description \"Find invoices\"", DiagnosticCodes.AgentOperationMissingScope)]
    [InlineData("agents\n  tokens personal\n  query All\n    scope read\n    description $strings.agent", DiagnosticCodes.InvalidAgentsDeclaration)]
    [InlineData("agents\n  tokens personal\n  query All\n    scope read\n    description \"Find invoices\"\n    approval required", DiagnosticCodes.AgentApprovalNotSupported)]
    [InlineData("agents\n  tokens personal\n  command Issue\n    scope read\n    description \"Issue an invoice\"", DiagnosticCodes.AgentCommandRequiresWriteScope)]
    [InlineData("agents\n  tokens personal\n  query Missing\n    scope read\n    description \"Find invoices\"", DiagnosticCodes.UnknownAgentOperation)]
    [InlineData("agents\n  tokens personal\n  query Issue\n    scope read\n    description \"Wrong kind\"", DiagnosticCodes.UnknownAgentOperation)]
    [InlineData("agents\n  tokens personal\n  query All\n    scope read\n    description \"Find invoices\"\n      scope write", DiagnosticCodes.InvalidAgentsDeclaration)]
    [InlineData("agents\n  tokens personal\n    scope read", DiagnosticCodes.InvalidAgentsDeclaration)]
    void should_report_an_error(string allowlist, string code)
    {
        var result = Compiler.Compile(Model + "\n" + allowlist);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    void should_refuse_an_exposed_global_query()
    {
        var source = Model.Replace("      query All => Invoice[]", "      query All => Invoice[]\n        scoped to global", StringComparison.Ordinal);
        var result = Compiler.Compile(source + "\n" + Allowlist);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.AgentGlobalQueryRefused && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    void should_refuse_a_second_block()
    {
        var result = Compiler.Compile(Model + "\n" + Allowlist + "\n" + Allowlist);
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.DuplicateAgentsDeclaration);
    }

    [Fact]
    void should_refuse_aliases_for_the_same_operation()
    {
        var result = Compiler.Compile(Model + "\n" + Allowlist + "\n  query Sales.Invoices.Listing.All\n    scope read\n    description \"Again\"");
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.DuplicateAgentOperation);
    }
}
