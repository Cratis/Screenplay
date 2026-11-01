// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.given;

public class an_every_published_history : a_published_history
{
    const string OtherProducer = """
              command ChangeOther
                item String identifier
                produces OtherChanged
                  for item
              event OtherChanged
        """;

    const string FromScope = """
              projection Publisher => PricePublished
                from PriceChanged
                  price = price
        """;

    const string EveryScope = """
              projection Publisher => PricePublished
                every
                  price = 10.5
        """;

    void Establish()
    {
        // An every-only scope cannot read event properties, so it publishes a constant; only one source type remains.
        var source = Source.Replace(OtherProducer + "\n", string.Empty).Replace(FromScope, EveryScope);
        Assert.NotEqual(Source, source);
        _model = Compile(source);
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
}
