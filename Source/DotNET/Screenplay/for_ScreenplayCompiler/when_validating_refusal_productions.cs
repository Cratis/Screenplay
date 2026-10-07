// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_validating_refusal_productions : given.a_compiler
{
    const string Source = """
        concept PersonalId : String @pii
        system Bank
        type Details
          message String
        module Billing
          feature Claims
            slice Automation Handling
              event Approved
                id PersonalId
              event Refused
                reason String
                details Details
              operation Refund
                uses Bank
              command Claim
              reaction Claimer
                when Approved
                  id
                  invokes Claim
                    on refused
        """;

    [Theory]
    [InlineData("Refund", "", DiagnosticCodes.OperationOutsideCommand)]
    [InlineData("Missing", "", DiagnosticCodes.UnknownEvent)]
    [InlineData("Billing.Claims.Handling.Missing", "", DiagnosticCodes.UnknownEvent)]
    [InlineData("Refused", "                missing = $refusal.reason", DiagnosticCodes.UnknownEventField)]
    [InlineData("Refused", "                details = { \"missing\": \"value\" }", DiagnosticCodes.UnknownStructuredValueMember)]
    [InlineData("Refused", "                details = [\"value\"]", DiagnosticCodes.IncompatibleStructuredValue)]
    [InlineData("Refused", "                for id", DiagnosticCodes.PiiNotSupportedOnIdentifier)]
    void should_apply_ordinary_production_validation(string target, string mapping, string code)
    {
        var result = _compiler.Compile(Source + $"\n              produces {target}\n{mapping}");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeTrue();
    }

    [Fact]
    void should_keep_qualified_branch_event_targets_valid()
    {
        var result = _compiler.Compile(Source + "\n              produces Billing.Claims.Handling.Refused\n                reason = $refusal.reason");
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    }

    [Fact]
    void should_reject_a_mixed_event_and_operation_target()
    {
        var source = Source.Replace("      operation Refund", "      operation Refund\n      event Refund", StringComparison.Ordinal);
        var result = _compiler.Compile(source + "\n              produces Refund");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidProductionReference).ShouldBeTrue();
    }
}
