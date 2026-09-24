// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_single_aliased_read : given.a_compiler
{
    const string Source =
        """
        module Banking
          feature Transfers
            slice StateChange Transfer
              command Transfer
                sourceId Uuid
                reads Account as source by sourceId
                validate
                  require source.balance > 0
                    message "Funds required"
                  require Account.active == true
                    message "Account must be active"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_accept_both_the_alias_and_the_unambiguous_view_name() =>
        _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRequirementOperandSource).ShouldEqual(0);
    [Fact] void should_keep_the_alias() =>
        _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Reads!.Single().Alias.ShouldEqual("source");
}
