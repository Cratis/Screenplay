// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_EventConsumerCompleteness;

public class when_a_consumer_resolves_one_of_two_same_named_events : Specification
{
    CompilationResult<ApplicationSyntax> _compilation;
    ImmutableArray<Diagnostic> _findings;

    void Establish()
    {
        _compilation = new ScreenplayCompiler().Compile("module M\n  feature F\n    slice StateView First\n      event Changed\n      projection P\n        from Changed\n    slice StateView Second\n      event Changed");
        _compilation.Diagnostics.ShouldBeEmpty();
    }

    void Because() => _findings = ModelCompleteness.Check(_compilation, new([CompletenessCheck.EventConsumers]));

    [Fact] void should_report_only_the_other_contract() => _findings.Single().Location.ShouldEqual(_compilation.Value!.Modules.Single().Features.Single().Slices.Last().Events.Single().Location);
}
