// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_repeated_reads_without_aliases : given.a_compiler
{
    const string Source =
        """
        module Banking
          feature Transfers
            slice StateChange Transfer
              command Transfer
                sourceId Uuid
                destinationId Uuid
                reads Account by sourceId
                reads Account as destination by destinationId
                validate
                  require Account.balance > 0
                    message "Balance required"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_require_an_alias_on_the_unaliased_instance() =>
        _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.MissingReadsAlias).ShouldEqual(1);
    [Fact] void should_not_treat_an_ambiguous_view_name_as_a_requirement_source() =>
        _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRequirementOperandSource).ShouldEqual(1);
    [Fact] void should_preserve_both_reads_for_a_correctable_document() =>
        _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Reads!.Count().ShouldEqual(2);
}
