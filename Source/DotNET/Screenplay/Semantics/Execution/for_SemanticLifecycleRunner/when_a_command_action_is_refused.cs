// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner;

public class when_a_command_action_is_refused : Specification
{
    const string Failing = "outcome ChargeLateFee failed\n            outcome Approve expired\n            then invokes ChargeFee\n            then invokes ChargeFee\n            then invokes ChargeFee\n            then invokes WriteOff\n            then completes";
    const string Refusing = "outcome ChargeLateFee refused\n            then invokes ChargeFee\n            then fails at ChargeLateFee";

    ImmutableArray<SemanticLifecycleSpecificationResult> _results;
    SemanticLifecycleSpecificationResult _refused;

    void Because()
    {
        var source = a_lifecycle_source.Source.Replace(Failing, Refusing, StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        var result = new SemanticModelBinder(2026).Bind("Billing", new ScreenplayCompiler().Parse(source).Value!, SemanticDocumentSet.Create([document], catalog));
        _results = SemanticLifecycleRunner.RunAll(result.Value!.Model);
        _refused = _results.Single(candidate => candidate.Name == "AStartedRunFails");
    }

    [Fact] void should_run_every_specification() => _results.Length.ShouldEqual(7);
    [Fact] void should_pass_the_refused_specification() => string.Join("; ", _refused.Failures).ShouldEqual(string.Empty);
}
