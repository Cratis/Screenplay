// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.given;

public class a_validated_command_plan : Specification
{
    const string Source =
        """
        concept Reference : String
          validate
            max 12 message "A reference is at most twelve characters"
        concept Quantity : Int
          validate
            min 1 message "Order at least one"
        concept Status : Enum
          open
          closed
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                name String
                note String?
                reference Reference
                quantities Quantity[]
                amount Decimal
                status Status
                priority Int
                discount Decimal
                code String
                weights Decimal[]
                validate
                  name min 2 message "A name has at least two characters"
                  name max 10
                  note min 3 message "A note has at least three characters"
                  amount max 1000 message "An order is at most 1000"
                  status != closed message "A closed order cannot be placed"
                  amount > 0 message "An order is for something"
                  priority >= 1 message "Priority starts at one"
                  priority < 6 message "Priority stops at five"
                  discount <= 100 message "A discount is at most 100"
                  code length == 3
                  weights all > 0 message "Every weight counts"
                  weights all >= 0.5 message "No weight is below a half"
        """;

    protected SemanticExecutionPlanCompilation _compilation;
    protected SemanticExecutionPlan _plan;
    protected SemanticCommand _command;

    void Establish()
    {
        const string StableKey = "validated-command";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "PlaceOrder.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        _compilation = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        _plan = _compilation.Plan!;
        _command = _plan.Commands.Values.Single();
    }

    protected SemanticExecutionResult Execute(params (string Property, SemanticValue Value)[] changes)
    {
        var values = new Dictionary<string, SemanticValue>(StringComparer.Ordinal)
        {
            ["orderId"] = SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ["name"] = SemanticValue.Text("Order"),
            ["note"] = SemanticValue.Null,
            ["reference"] = SemanticValue.Text("REF-1"),
            ["quantities"] = SemanticValue.Array([SemanticValue.Number(1), SemanticValue.Number(2)]),
            ["amount"] = SemanticValue.Number(10),
            ["status"] = SemanticValue.Text("open"),
            ["priority"] = SemanticValue.Number(1),
            ["discount"] = SemanticValue.Number(0),
            ["code"] = SemanticValue.Text("NOK"),
            ["weights"] = SemanticValue.Array([SemanticValue.Number(0.5m), SemanticValue.Number(2)])
        };
        foreach (var (property, value) in changes)
        {
            values[property] = value;
        }

        var request = SemanticExecutionRequest.Create(
            _command.Id,
            [.. _command.Properties.Select(property => new SemanticPropertyValue(property.Id, values[property.Name]))],
            []);
        return new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, request);
    }
}
