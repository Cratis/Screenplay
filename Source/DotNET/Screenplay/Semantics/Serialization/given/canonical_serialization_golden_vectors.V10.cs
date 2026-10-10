// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV10Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v10.json");

    public static ExecutableSemanticModel CreateSemanticModelV10()
    {
        var application = CreateSemanticModelV8().Application;
        var source = application.EventSources.Single(source => source.Id == Id(9000));
        var feature = application.Modules[0].Features.Single(feature => feature.Id == Id(9300));
        var slice = feature.Slices[0];
        var commands = slice.Commands.ToBuilder();
        commands[0] = commands[0] with { Produces = [commands[0].Produces[0] with { Route = new(source.Id, Id(9002)) { StreamId = SemanticExpression.FromValue(SemanticValue.Text("override")) } }] };
        commands[1] = commands[1] with { Produces = [commands[1].Produces[0] with { Route = new(source.Id, Id(9005)) { StreamIdParts = [new("projectId", SemanticExpression.Property(SemanticExpressionRootKind.Command, commands[1].Properties[0].Id)), new("period", SemanticExpression.FromValue(SemanticValue.Text("month")))] } }] };
        commands[2] = commands[2] with { Route = null, Produces = [commands[2].Produces[0] with { Route = new(source.Id, Id(9004)) }] };
        var text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
        var readModel = new SemanticReadModel(Id(10010), "FilteredBalance", [new(Id(10011), "id", text, true)]);
        var reducer = new SemanticReducer("FilteredReducer", readModel.Id, [new(slice.Events[0].Id, "filtered-transition")]) { From = new(source.Id, Id(9002)) };
        var automation = new SemanticSlice(Id(10000), "FilteredAutomation", SemanticSliceKind.Automation, [], [], [], [], [], [])
        {
            Reactions = [
                new(Id(10001), "SourceFilter", [new(SemanticReactionTriggerKind.Event) { Source = slice.Events[0].Id }]) { From = new(source.Id) },
                new(Id(10002), "StreamFilter", [new(SemanticReactionTriggerKind.Event) { Source = slice.Events[0].Id }]) { From = new(source.Id, Id(9002)) }
            ]
        };
        var replacement = feature with { Slices = [slice with { Commands = commands.ToImmutable(), ReadModels = [readModel], Reducers = [reducer] }, automation] };
        var module = application.Modules[0];
        application = application with { Modules = application.Modules.SetItem(0, module with { Features = [.. module.Features.Select(value => value.Id == replacement.Id ? replacement : value)] }) };

        return ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, application);
    }
}
#endif
