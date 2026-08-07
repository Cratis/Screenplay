// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_resolving_a_screen_reference;

public class and_two_siblings_match_equally_well : given.a_compiler
{
    // The aggregating screen - it binds a read model from a sibling slice, and 'All' names two of them at
    // the same depth. Nothing in the document says which, so the compiler will not pick one for the reader.
    const string Source =
        """
        module Invoicing
          feature Preparation
            slice StateView Queue
              query All => QueueReadModel

            slice StateView Deviations
              query All => DeviationReadModel

            slice StateView Overview
              screen OverviewScreen
                data QueueReadModel[] via query All
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed_with_a_warning() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_ambiguity() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.AmbiguousReference);
    [Fact] void should_report_it_as_a_warning() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);

    // The message names the candidates, because "ambiguous" without them leaves the reader to go looking.
    [Fact] void should_name_both_candidates() =>
        _result.Diagnostics.Single().Message.ShouldContain("Invoicing.Preparation.Queue, Invoicing.Preparation.Deviations");
    [Fact] void should_say_how_to_settle_it() => _result.Diagnostics.Single().Message.ShouldContain("qualify it");
}
