// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_reads_alias_that_collides_with_a_property : given.a_compiler
{
    const string Source =
        """
        module Banking
          feature Transfers
            slice StateChange Transfer
              command Transfer
                source Uuid
                reads Account as source by source
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_property_collision() =>
        _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.ReadsAliasConflictsWithProperty).ShouldEqual(1);
}
