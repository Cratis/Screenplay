// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticPolicyEvaluation.given;

public class a_plan_gated_command : Specification
{
    internal const string Source = """
        policy DataQualityEnabled
          reads AssetPlan as plan by assetId
          require authenticated and plan.dataQuality == true
        module Fleet
          feature Tokens
            slice StateChange Request
              event TokenRequested
                assetId String
              command RequestToken
                assetId String identifier
                authorize DataQualityEnabled
                produces TokenRequested
                  for assetId
                  assetId = assetId
            slice StateView Plans
              readmodel AssetPlan
                assetId String key
                dataQuality Bool
              event PlanSet
                dataQuality Bool
              projection Build => AssetPlan
                from PlanSet
                  assetId = $eventSourceId
                  dataQuality = dataQuality
        """;

    protected SemanticExecutionPlan Plan = null!;
    protected SemanticCommand Command = null!;
    protected SemanticReadModel ReadModel = null!;
    protected SemanticProperty KeyProperty = null!;
    protected SemanticProperty PlanFlag = null!;
    protected SemanticExecutionResult Result = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Fleet"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("fleet"), "fleet", "fleet.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Fleet", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
        Plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        Command = Plan.Commands.Values.Single();
        ReadModel = Plan.ReadModels.Values.Single(model => model.Name == "AssetPlan");
        KeyProperty = ReadModel.Properties.Single(property => property.Name == "assetId");
        PlanFlag = ReadModel.Properties.Single(property => property.Name == "dataQuality");
    }

    protected SemanticWorld WorldWithPlan(string assetId, bool dataQuality) => SemanticWorld.Create(
        [],
        [new SemanticReadModelInstance(ReadModel.Id, SemanticValue.Text(assetId), [new(KeyProperty.Id, SemanticValue.Text(assetId)), new(PlanFlag.Id, SemanticValue.Boolean(dataQuality))])]);

    protected SemanticExecutionResult Execute(SemanticWorld world, bool authenticated = true) =>
        new SemanticEvaluator().Execute(
            Plan,
            world,
            SemanticExecutionRequest.Create(Command.Id, [new(Command.Properties.Single(property => !property.IsGenerated).Id, SemanticValue.Text("vessel-1"))], []) with
            {
                Caller = new(authenticated, [], [])
            });
}
