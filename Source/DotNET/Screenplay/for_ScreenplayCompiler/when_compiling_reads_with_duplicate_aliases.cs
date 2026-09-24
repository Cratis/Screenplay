// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_reads_with_duplicate_aliases : given.a_compiler
{
    const string Source =
        """
        module Banking
          feature Transfers
            slice StateChange Transfer
              command Transfer
                sourceId Uuid
                destinationId Uuid
                reads Account as account by sourceId
                reads Ledger as account by destinationId
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_duplicate_alias() =>
        _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateReadsAlias).ShouldEqual(1);
}
