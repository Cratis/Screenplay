// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.given;

public class a_published_history : Specification
{
    protected const string Source = """
        module M
          feature F
            slice StateChange Pricing
              command ChangePrice
                product Uuid identifier
                price Decimal
                produces PriceChanged
                  for product
                  price = price
              event PriceChanged
                price Decimal
              command ChangeOther
                item String identifier
                produces OtherChanged
                  for item
              event OtherChanged
            slice Translate Publication
              direction outbound
              public event PricePublished
                price Decimal
              projection Publisher => PricePublished
                from PriceChanged
                  price = price
            slice StateView View
              readmodel Summary
                product Uuid key
                price Decimal
              projection SummaryProjection => Summary
                from PriceChanged
                  price = price
              specification existing_price
                given PriceChanged
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  price = 10.5
                then readmodel Summary
                  product = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  price = 10.5
        """;

    protected ExecutableSemanticModel _model;
    protected SemanticExecutionPlan _plan;
    protected SemanticExecutionRequest _request;
    protected SemanticAccepted _live;
    protected SemanticFact _published;

    void Establish()
    {
        _model = Compile(Source);
        _plan = Plan(_model);
        var command = _plan.Commands.Values.Single(command => command.Name == "ChangePrice");
        _request = SemanticExecutionRequest.Create(
            command.Id,
            [
                new(command.Properties.Single(property => property.Name == "product").Id, SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6")),
                new(command.Properties.Single(property => property.Name == "price").Id, SemanticValue.Number(10.5m))
            ],
            []);
        _live = (SemanticAccepted)new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, _request);
        _published = _live.Facts.Single(fact => _plan.Events[fact.EventContract].Name == "PricePublished");
    }

    protected static ExecutableSemanticModel Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Publication"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("publication"), "publication", "Publication.play", source);
        var result = new SemanticModelCompiler().Compile("Publication", SemanticDocumentSet.Create([document], catalog));
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value!.Model;
    }

    protected static SemanticExecutionPlan Plan(ExecutableSemanticModel model)
    {
        var result = SemanticExecutionPlan.Compile(model);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Details)));
        return result.Plan!;
    }
}
