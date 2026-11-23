// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors.given;

public class revised_lifecycle_models : golden_files
{
    protected static readonly ImmutableArray<string> CollectSpecifications =
        ["NoPaymentIsWrittenOff", "AFailedFeeFailsNothingButAsksForApproval", "AStartedRunFails"];

    protected static ExecutableSemanticModel Revise36()
    {
        var original = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(36).Bytes.AsSpan());
        var application = original.Application with
        {
            Workflows = [.. original.Application.Workflows.Select(workflow => workflow.Name != "Collect" ? workflow : workflow with
            {
                Specifications = [.. workflow.Specifications.Select(specification => !CollectSpecifications.Contains(specification.Name) ? specification : specification with
                {
                    Then = [.. specification.Then.Select(expectation => expectation.Kind != SemanticLifecycleExpectationKind.Completes ? expectation :
                        new SemanticLifecycleExpectation(SemanticLifecycleExpectationKind.Waits, "Final"))]
                })]
            })]
        };

        return ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, application, 2026);
    }

    protected static ExecutableSemanticModel Revise57()
    {
        var original = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(57).Bytes.AsSpan());
        var packed = original.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Events).Single(contract => contract.Name == "OrderPacked");
        var parcelId = packed.Properties.Single(property => property.Name == "parcelId").Id;
        SemanticLifecycleSpecification Fill(SemanticLifecycleSpecification specification) => specification with
        {
            WhenAppended = specification.WhenAppended! with
            {
                Values = [new SemanticPropertyValue(parcelId, specification.WhenAppended.EventSource!.Value)]
            }
        };
        var application = original.Application with
        {
            StateMachines = [.. original.Application.StateMachines.Select(machine => machine with { Specifications = [.. machine.Specifications.Select(Fill)] })],
            Workflows = [.. original.Application.Workflows.Select(workflow => workflow with { Specifications = [.. workflow.Specifications.Select(Fill)] })]
        };

        return ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, application, 2026);
    }

    protected void Regenerate(int version, ExecutableSemanticModel model)
    {
        var variable = $"SCREENPLAY_REGENERATE_LIFECYCLE_V{version}_R2";
        if (Environment.GetEnvironmentVariable(variable) != "1") return;
        File.WriteAllBytes(Path.Combine(_goldenFolder, $"lifecycle-esm-v{version}-r2.json"), SemanticModelSerializer.Serialize(model));
        Assert.Fail($"Lifecycle V{version}R2 regenerated; review, pin its hash and revision, rebuild and rerun without {variable}.");
    }
}
