// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_documented_destinations_and_rejections : given.a_semantic_binder
{
    const string Source =
        """
        module Requests
          feature Activation
            slice StateChange ActivateRequest
              command Activate
                requestId Uuid identifier
                contractId Uuid
                produces RequestActivated
                  requestId = requestId
                produces ContractPolicyActivated
                  for requestId
                  contractId = contractId
              event RequestActivated
                requestId Uuid
              event ContractPolicyActivated
                contractId Uuid
              specification RejectingActivation
                when Activate
                  requestId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  contractId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then error "Not allowed"
        """;

    CompilationResult<SemanticCompilation> _accepted;
    CompilationResult<SemanticCompilation> _fanOut;
    CompilationResult<SemanticCompilation> _multipleErrors;

    void Because()
    {
        _accepted = Bind(Source);
        _fanOut = Bind(Source.Replace("for requestId", "for contractId", StringComparison.Ordinal));
        _multipleErrors = Bind(Source.Replace("then error \"Not allowed\"", "then error \"Not allowed\"\n                then error", StringComparison.Ordinal));
    }

    [Fact] void should_bind_the_documented_destination_and_single_rejection() => _accepted.Success.ShouldBeTrue();
    [Fact] void should_explain_why_fan_out_cannot_bind() => _fanOut.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("command's required scalar identifier", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_explain_why_multiple_rejections_cannot_bind() => _multipleErrors.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("exactly one 'then error'", StringComparison.Ordinal)).ShouldBeTrue();
}
