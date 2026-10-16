// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.given;

public class a_date_command : for_SemanticModelBinder.when_binding_date_comparisons.given.a_date_comparison
{
    protected SemanticExecutionResult Execute(string source, string from, string? to)
    {
        var bound = Bind(source);
        Assert.True(bound.Success, string.Join('\n', bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = bound.Value!.Model;
        var command = Command(model);
        var values = command.Properties.Select(property => new SemanticPropertyValue(property.Id, property.Name switch
        {
            "id" => SemanticValue.Text("window"),
            "from" => SemanticValue.Text(from),
            _ => to is null ? SemanticValue.Null : SemanticValue.Text(to)
        }));
        var request = SemanticExecutionRequest.Create(command.Id, [.. values], []);

        return new SemanticEvaluator().Execute(SemanticExecutionPlan.Compile(model).Plan!, SemanticWorld.Empty, request);
    }
}
