// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Api;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Ui;

// Reads every screen into the neutral contract, then keys its references to executable identities. A reference the
// executable model does not hold is refused, never dropped.
sealed class ExecutableScreenResolver(ApplicationSyntax syntax, SemanticCompilation compilation)
{
    readonly List<string> _refusals = [];
    readonly Dictionary<ScreenSyntax, SemanticId> _screenIds = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<SliceSyntax, DeclarationScope> _scopes = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<SemanticId, SemanticCommand> _commands = [];
    readonly Dictionary<string, (ImmutableArray<string> Path, SemanticSlice Slice)> _slices = new(StringComparer.Ordinal);
    ConsistencyDeclarations? _declarations;

    internal ImmutableArray<string> Refusals => [.. _refusals.Distinct(StringComparer.Ordinal)];

    internal Dictionary<SemanticId, ImmutableArray<SemanticScreen>> Resolve()
    {
        var model = compilation.Model;
        foreach (var (path, slice) in ApiSlices.All(model.Application))
        {
            _slices[string.Join('.', path)] = (path, slice);
            foreach (var command in slice.Commands) _commands[command.Id] = command;
        }

        var scoped = ScreenplayValidator.ScopedSlices(syntax).ToArray();
        foreach (var (slice, scope) in scoped) _scopes[slice] = scope;
        _declarations = new ConsistencyDeclarations(syntax, scoped);
        var index = new UiModelIndex(syntax, model);
        var read = index.Slices
            .Where(entry => _slices.ContainsKey(entry.Path))
            .SelectMany(entry => entry.Slice.Screens.Select(screen => (entry.Path, entry.Slice, Screen: new UiScreenReader(index, entry.Path).Read(screen), Id: ScreenId(entry.Path, screen))))
            .ToArray();

        return read
            .GroupBy(entry => _slices[entry.Path].Slice.Id)
            .ToDictionary(group => group.Key, group => group.Select(entry => Screen(entry.Path, entry.Slice, entry.Id, entry.Screen)).ToImmutableArray());
    }

    static ImmutableArray<SemanticScreenRegion> Regions(IEnumerable<UiRegion> regions) =>
        [.. regions.Select(region => new SemanticScreenRegion(region.Kind, Regions(region.Regions))
        {
            Name = region.Name,
            Shows = region.Shows,
            Title = region.Title,
            Actions = [.. region.Actions]
        })];

    SemanticId ScreenId(string slice, ScreenSyntax screen)
    {
        var name = screen.Name;
        var path = _slices[slice].Path;
        var catalog = compilation.Documents.IdentityCatalog;
        var sliceAddress = SemanticAddress.ForSlice(catalog.Application, path[0], path[1..^1], path[^1]);
        var id = catalog.ResolveSemanticAssignment(SemanticAddress.ForScreen(sliceAddress, name)).Id;
        _screenIds[screen] = id;
        return id;
    }

    SemanticScreen Screen(string slice, SliceSyntax declaring, SemanticId id, UiScreen screen)
    {
        var owner = $"Screen '{screen.Name}' in '{slice}'";
        return new(
            id,
            screen.Name,
            screen.Title,
            [.. screen.Data.Select(data => new SemanticScreenData(Identity(data.QueryId, owner, $"shows query '{data.Query}'"), [.. data.Fields.Select(field => new SemanticScreenField(field.Property, field.Label))]) { By = data.By })],
            [.. screen.Actions.Select(action => Action(declaring, owner, action))],
            [.. screen.Navigation.Select(navigation => new SemanticScreenNavigation(Target(declaring, owner, navigation.Screen), navigation.Trigger) { By = navigation.By, Opens = navigation.Opens })],
            new([.. screen.Layout.Fits], Regions(screen.Layout.Regions)))
        {
            Description = screen.Description,
            Extensions = [.. (screen.Extensions ?? []).Select(need => new SemanticScreenExtension(System(owner, need.System), need.Reference))],
            Components = [.. (screen.Scene?.Components ?? []).Where(component => component.Extension is not null).Select(component => ExtensionComponent(declaring, owner, component))]
        };
    }

    SemanticScreenComponent ExtensionComponent(SliceSyntax declaring, string owner, UiSceneComponent component)
    {
        var extension = component.Extension!;
        return new(
            component.Name,
            System(owner, extension.System),
            extension.Component,
            [.. component.Properties.Keys],
            [.. (extension.Handles ?? []).Select(handler => new SemanticScreenSignalHandler(handler.Signal, [.. (handler.Commands ?? []).Select(command => HandledCommand(declaring, owner, command))]))]);
    }

    SemanticId System(string owner, string name)
    {
        var systems = compilation.Model.Application.Systems.Where(system => system.Name == name).ToArray();
        if (systems.Length == 1) return systems[0].Id;
        _refusals.Add($"{owner} places a component through system '{name}', which the executable model does not declare.");
        return default;
    }

    SemanticId HandledCommand(SliceSyntax declaring, string owner, string name)
    {
        var inSlice = _slices.Values.Where(entry => entry.Slice.Name == declaring.Name).SelectMany(entry => entry.Slice.Commands).Where(command => command.Name == name).ToArray();
        var candidates = inSlice.Length > 0 ? inSlice : [.. _commands.Values.Where(command => command.Name == name)];
        if (candidates.Length == 1) return candidates[0].Id;
        _refusals.Add($"{owner} handles a signal with command '{name}', which {(candidates.Length == 0 ? "no slice declares" : "more than one slice declares")}.");
        return default;
    }

    SemanticScreenAction Action(SliceSyntax slice, string owner, UiAction action)
    {
        var command = Identity(action.CommandId, owner, $"issues command '{action.Command}'");
        var properties = _commands.TryGetValue(command, out var found) ? found.Properties : [];
        var inputs = action.Inputs.Select(input =>
        {
            var property = properties.FirstOrDefault(candidate => candidate.Name == input.Name);
            if (property is null) _refusals.Add($"{owner} binds input '{input.Name}' that command '{action.Command}' does not declare.");
            return new SemanticScreenInput(property?.Id ?? default, input.From);
        });
        return new(command, action.Label, action.Subject, [.. inputs])
        {
            When = action.When,
            Then = action.Then is { } then ? Target(slice, owner, then) : null
        };
    }

    SemanticId Target(SliceSyntax slice, string owner, string name)
    {
        if (_declarations!.Resolve(name, _scopes[slice], candidate => candidate.Screens, screen => screen.Name) is { } resolved &&
            _screenIds.TryGetValue(resolved.Node, out var id))
        {
            return id;
        }

        _refusals.Add($"{owner} leads to '{name}', which does not resolve to exactly one screen of the executable model.");
        return default;
    }

    SemanticId Identity(string? id, string owner, string reference)
    {
        if (id is not null) return SemanticId.Parse(id);
        _refusals.Add($"{owner} {reference}, which the executable model does not hold.");
        return default;
    }
}
