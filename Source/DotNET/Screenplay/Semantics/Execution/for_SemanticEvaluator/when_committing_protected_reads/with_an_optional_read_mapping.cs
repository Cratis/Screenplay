// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_an_optional_read_mapping : given.a_protected_command
{
    void Establish()
    {
        Compile(Source.Replace("value String", "value String optional", StringComparison.Ordinal)
            .Replace("        validate\n          require View.value != \"\"\n            message \"Value is empty.\"\n", string.Empty, StringComparison.Ordinal));
        var recorded = _plan.Events.Values.Single(value => value.Name == "Recorded");
        _world = World(new SemanticFact(recorded.Id, SemanticValue.Text("read"), [new(recorded.Properties.Single().Id, SemanticValue.Null)])
        {
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("read")))
        });
    }

    void Because() => _result = _evaluator.Execute(_plan, _world, Request());

    [Fact] void should_map_null_using_existing_optional_value_rules() => ((SemanticAccepted)_result).Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Null);
}
