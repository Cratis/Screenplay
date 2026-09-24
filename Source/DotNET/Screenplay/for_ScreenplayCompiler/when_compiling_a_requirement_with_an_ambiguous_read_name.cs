// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_requirement_with_an_ambiguous_read_name : given.a_compiler
{
    const string Source =
        """
        module Banking
          feature Transfers
            slice StateChange Transfer
              command Transfer
                sourceId Uuid
                destinationId Uuid
                reads Account as source by sourceId
                reads Account as destination by destinationId
                validate
                  require Account.balance > 0
                    message "Balance required"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_ambiguity_with_aliases_in_declaration_order() =>
        _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRequirementOperandSource).Message
            .ShouldEqual("Command 'Transfer' requires 'Account.balance', but reads 'Account' more than once; qualify the path with one of its aliases (source, destination).");
    [Fact] void should_not_report_a_missing_alias() =>
        _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.MissingReadsAlias).ShouldEqual(0);
}
