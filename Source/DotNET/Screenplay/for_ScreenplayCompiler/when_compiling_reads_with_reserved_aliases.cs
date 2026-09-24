// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_reads_with_reserved_aliases : given.a_compiler
{
    const string Source =
        """
        module Banking
          feature Transfers
            slice StateChange Transfer
              command Transfer
                accountId Uuid
                reads Account as as by accountId
                reads Account as by by accountId
                reads Account as reads by accountId
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_reject_each_reserved_alias_with_a_specific_message() =>
        _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadsDeclaration)
            .Select(diagnostic => diagnostic.Message).ShouldEqual(
                "Invalid reads declaration 'reads Account as as by accountId' - 'as' cannot be used as a reads alias",
                "Invalid reads declaration 'reads Account as by by accountId' - 'by' cannot be used as a reads alias",
                "Invalid reads declaration 'reads Account as reads by accountId' - 'reads' cannot be used as a reads alias");
}
