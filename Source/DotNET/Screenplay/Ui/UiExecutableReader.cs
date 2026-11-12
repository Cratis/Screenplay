// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Api;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Ui;

// Reads the neutral UI contract from the screens an ESM v42 model holds, with nothing but the executable model.
sealed class UiExecutableReader
{
    static readonly Dictionary<SemanticPrimitiveType, string> _primitives = new()
    {
        [SemanticPrimitiveType.Uuid] = "Uuid",
        [SemanticPrimitiveType.Text] = "String",
        [SemanticPrimitiveType.WholeNumber] = "Int",
        [SemanticPrimitiveType.DecimalNumber] = "Decimal",
        [SemanticPrimitiveType.Boolean] = "Bool",
        [SemanticPrimitiveType.Date] = "Date",
        [SemanticPrimitiveType.DateTime] = "DateTime"
    };

    readonly ExecutableSemanticModel _model;
    readonly Dictionary<SemanticId, string> _typeNames = [];
    readonly Dictionary<SemanticId, SemanticKeyedQuery> _queries = [];
    readonly Dictionary<SemanticId, SemanticReadModel> _readModels = [];
    readonly Dictionary<SemanticId, SemanticCommand> _commands = [];
    readonly Dictionary<SemanticId, SemanticScreen> _screens = [];
    readonly Dictionary<SemanticId, ApiOperation> _operations = [];

    internal UiExecutableReader(ExecutableSemanticModel model)
    {
        _model = model;
        foreach (var concept in model.Application.Concepts) _typeNames[concept.Id] = concept.Name;
        foreach (var type in model.Application.Types) _typeNames[type.Id] = type.Name;
        foreach (var (_, slice) in ApiSlices.All(model.Application))
        {
            foreach (var query in slice.Queries) _queries[query.Id] = query;
            foreach (var readModel in slice.ReadModels) _readModels[readModel.Id] = readModel;
            foreach (var command in slice.Commands) _commands[command.Id] = command;
            foreach (var screen in slice.Screens) _screens[screen.Id] = screen;
        }

        foreach (var operation in ApiDescription.For(model).Operations) _operations[operation.Id] = operation;
    }

    internal UiContract Read()
    {
        var screens = ApiSlices.All(_model.Application)
            .SelectMany(entry => entry.Slice.Screens.Select(screen => Screen(string.Join('.', entry.Path), entry.Slice.Id, screen)))
            .ToArray();
        return new(UiContract.Version, _model.Application.Name, screens) { ModelRevision = _model.Revision.ToString() };
    }

    static UiRegion Region(SemanticScreenRegion region) =>
        new(region.Kind, [.. region.Regions.Select(Region)])
        {
            Name = region.Name,
            Shows = region.Shows,
            Title = region.Title,
            Actions = [.. region.Actions]
        };

    UiScreen Screen(string slice, SemanticId sliceId, SemanticScreen screen) =>
        new(
            screen.Name,
            slice,
            screen.Title,
            [.. screen.Data.Select(Data)],
            [.. screen.Actions.Select(Action)],
            [.. screen.Navigation.Select(navigation => new UiNavigation(_screens[navigation.Screen].Name, navigation.Trigger) { By = navigation.By, Opens = navigation.Opens })],
            new([.. screen.Layout.Fits], [.. screen.Layout.Regions.Select(Region)]))
        {
            SliceId = sliceId.ToString(),
            Description = screen.Description,
            Extensions = screen.Extensions.IsDefaultOrEmpty ? null : [.. screen.Extensions.Select(extension => new UiExtensionNeed(SystemName(extension.System), extension.Reference))],
            Scene = screen.Components.IsDefaultOrEmpty ? null : new([], [.. screen.Components.Select(component => ExtensionComponent(screen, component))], [], [])
        };

    UiSceneComponent ExtensionComponent(SemanticScreen screen, SemanticScreenComponent component)
    {
        var system = SystemName(component.System);
        var reference = screen.Extensions.First(extension => extension.System == component.System).Reference;
        return new($"{system}.{component.Component}", component.Name, component.Properties.ToDictionary(property => property, _ => (JsonNode?)null, StringComparer.Ordinal), new Dictionary<string, string>())
        {
            Extension = new(system, reference, component.Component)
            {
                Handles = component.Handles.IsDefaultOrEmpty
                    ? null
                    : [.. component.Handles.Select(handler => new UiSignalHandler(handler.Signal) { Commands = handler.Commands.IsDefaultOrEmpty ? null : [.. handler.Commands.Select(command => _commands[command].Name)] })]
            }
        };
    }

    string SystemName(SemanticId system) => _model.Application.Systems.First(candidate => candidate.Id == system).Name;

    UiData Data(SemanticScreenData data)
    {
        var query = _queries[data.Query];
        var operation = _operations.GetValueOrDefault(query.Id);
        return new(query.Name, _readModels[query.ReadModel].Name, query.Cardinality == SemanticQueryCardinality.Many, [.. data.Fields.Select(field => new UiField(field.Property, field.Label))])
        {
            By = data.By,
            QueryId = query.Id.ToString(),
            Route = operation?.Route,
            Live = operation?.Live ?? false
        };
    }

    UiAction Action(SemanticScreenAction action)
    {
        var command = _commands[action.Command];
        var properties = command.Properties.ToDictionary(property => property.Id);
        return new(command.Name, action.Label, action.Subject, [.. action.Inputs.Select(input => new UiInput(properties[input.Property].Name, TypeName(properties[input.Property].Type), input.From))])
        {
            When = action.When,
            Then = action.Then is { } then ? _screens[then].Name : null,
            CommandId = command.Id.ToString(),
            Route = _operations.GetValueOrDefault(command.Id)?.Route
        };
    }

    string TypeName(SemanticTypeReference type)
    {
        var name = type.Kind == SemanticTypeReferenceKind.Primitive ? _primitives[type.Primitive] : _typeNames[type.Target];
        return $"{name}{(type.IsCollection ? "[]" : string.Empty)}{(type.IsOptional ? "?" : string.Empty)}";
    }
}
