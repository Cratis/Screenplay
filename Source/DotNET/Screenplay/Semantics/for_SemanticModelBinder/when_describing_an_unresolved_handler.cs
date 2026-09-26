// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_describing_an_unresolved_handler : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("""
        module Billing
          feature Accounts
            slice StateChange Commands
              command Deposit
                amount MissingConcept
                handler
                  file Handler.cs
        """);

    [Fact] void should_not_publish_a_wrapper_ready_context() => _result.TypedContextDescriptors.ShouldBeEmpty();
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
}
