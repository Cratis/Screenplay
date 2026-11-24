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
    readonly Dictionary<ScreenSyntax, DeclarationScope> _freeScopes = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<SemanticId, SemanticCommand> _commands = [];
    readonly Dictionary<string, (ImmutableArray<string> Path, SemanticSlice Slice)> _slices = new(StringComparer.Ordinal);
    ConsistencyDeclarations? _declarations;

    internal ImmutableArray<string> Refusals => [.. _refusals.Distinct(StringComparer.Ordinal)];

    // The free screens an adopting slice binds, and the adoptions that bind them. A free screen no slice adopts is design-only and
    // enters no executable model.
    internal ImmutableArray<SemanticScreen> FreeScreens { get; private set; } = [];

    internal Dictionary<SemanticId, ImmutableArray<SemanticScreenAdoption>> Adoptions { get; } = [];

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
            .SelectMany(entry => entry.Slice.Screens.Select(screen => (entry.Path, entry.Slice, Screen: new UiScreenReader(index, entry.Path).Read(screen), Id: ScreenId(entry.Path, screen), Syntax: screen)))
            .ToArray();

        var free = ResolveFree(index);
        var resolved = read
            .GroupBy(entry => _slices[entry.Path].Slice.Id)
            .ToDictionary(group => group.Key, group => group.Select(entry => Screen(entry.Path, entry.Slice, _scopes[entry.Slice], entry.Id, entry.Screen, entry.Syntax)).ToImmutableArray());
        FreeScreens = [.. free.Select(entry => Screen(string.Join('.', entry.Scope.Segments), null, entry.Scope, entry.Id, entry.Read, entry.Screen, entry.Needs) with { Scope = entry.Scope.Segments.Count == 0 ? null : string.Join('.', entry.Scope.Segments) })];
        BindAdoptions(index, free);
        return resolved;
    }

    static ImmutableArray<SemanticScreenRegion> Regions(IEnumerable<UiRegion> regions) =>
        [.. regions.Select(region => new SemanticScreenRegion(region.Kind, Regions(region.Regions))
        {
            Name = region.Name,
            Shows = region.Shows,
            Title = region.Title,
            Actions = [.. region.Actions]
        })];

    // Reads each free screen a slice adopts once, from where it is declared, and gives it the identity of a screen declared free.
    List<FreeScreen> ResolveFree(UiModelIndex index)
    {
        var scoped = ScreenplayValidator.ScopedScreens(syntax).ToList();
        var pool = scoped.ConvertAll(entry => new Declaration(entry.Screen.Name, entry.Scope));
        var adopted = new HashSet<ScreenSyntax>(ReferenceEqualityComparer.Instance);
        foreach (var (slice, scope) in _scopes)
        {
            foreach (var used in slice.UsedScreens)
            {
                if (ReferenceResolver.Resolve(used.Screen, scope, pool).Resolved is { } resolved) adopted.Add(scoped[pool.IndexOf(resolved)].Screen);
            }
        }

        var result = new List<FreeScreen>();
        var catalog = compilation.Documents.IdentityCatalog;
        foreach (var entry in scoped.Where(entry => !entry.InSlice && adopted.Contains(entry.Screen)))
        {
            var id = catalog.ResolveSemanticAssignment(SemanticAddress.ForFreeScreen(catalog.Application, entry.Scope.Segments, entry.Screen.Name)).Id;
            _screenIds[entry.Screen] = id;
            _freeScopes[entry.Screen] = entry.Scope;
            var reader = new UiScreenReader(index, string.Join('.', entry.Scope.Segments), null, string.Join('.', entry.Scope.Segments));
            var read = reader.Read(entry.Screen);
            var needs = UiScreenReader.NeedsOf(entry.Screen).Select(need => new SemanticScreenNeed(need.Name, need.Type.Name, need.Type.IsCollection, [.. reader.FieldsOf(need.Name, need.Type.Name).Select(field => new SemanticScreenField(field.Property, field.Label))])).ToImmutableArray();
            result.Add(new(entry.Screen, entry.Scope, id, read, needs));
        }

        return result;
    }

    // A slice's adoption keeps the free screen's identity and the query that provides each need; a need no provision provides refuses admission.
    void BindAdoptions(UiModelIndex index, List<FreeScreen> free)
    {
        var scoped = ScreenplayValidator.ScopedScreens(syntax).ToList();
        var pool = scoped.ConvertAll(entry => new Declaration(entry.Screen.Name, entry.Scope));
        foreach (var (path, slice) in index.Slices.Where(entry => _slices.ContainsKey(entry.Path)))
        {
            var sliceId = _slices[path].Slice.Id;
            var adoptions = ImmutableArray.CreateBuilder<SemanticScreenAdoption>();
            foreach (var used in slice.UsedScreens)
            {
                if (ReferenceResolver.Resolve(used.Screen, _scopes[slice], pool).Resolved is not { } resolved) continue;
                var found = free.Find(entry => ReferenceEquals(entry.Screen, scoped[pool.IndexOf(resolved)].Screen));
                if (found is null) continue;
                var provisions = ImmutableArray.CreateBuilder<SemanticScreenProvision>();
                foreach (var provision in used.Provisions)
                {
                    var query = index.Query(path, provision.Query);
                    var operation = query is { } declared ? index.Operation(declared.Slice, declared.Query.Name) : null;
                    var queryId = operation?.Id.ToString();
                    if (queryId is null)
                    {
                        _refusals.Add($"Screen '{found.Screen.Name}' adopted by '{path}' needs '{provision.Need}', provided by query '{provision.Query}', which the executable model does not hold.");
                        continue;
                    }

                    provisions.Add(new(provision.Need, SemanticId.Parse(queryId), [.. provision.Mappings.Select(mapping => new SemanticScreenProvisionField(mapping.Field, mapping.Path))]) { By = provision.By });
                }

                foreach (var need in found.Needs.Where(need => !provisions.Any(provision => provision.Need == need.Name)))
                {
                    _refusals.Add($"Screen '{found.Screen.Name}' adopted by '{path}' needs '{need.Name}' of shape '{need.Shape}', and slice '{slice.Name}' does not provide it.");
                }

                adoptions.Add(new(found.Id, provisions.ToImmutable()));
            }

            if (adoptions.Count > 0) Adoptions[sliceId] = adoptions.ToImmutable();
        }
    }

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

    SemanticScreen Screen(string slice, SliceSyntax? declaring, DeclarationScope scope, SemanticId id, UiScreen screen, ScreenSyntax screenSyntax, ImmutableArray<SemanticScreenNeed> needs = default)
    {
        var owner = declaring is null ? $"Free screen '{screen.Name}'" : $"Screen '{screen.Name}' in '{slice}'";
        return new(
            id,
            screen.Name,
            screen.Title,
            [.. screen.Data.Select(data => new SemanticScreenData(Identity(data.QueryId, owner, $"shows query '{data.Query}'"), [.. data.Fields.Select(field => new SemanticScreenField(field.Property, field.Label))]) { By = data.By, Available = SemanticAvailabilityBinder.Gates(data.Available) })],
            [.. screen.Actions.Select(action => Action(declaring, scope, owner, action))],
            [.. screen.Navigation.Select(navigation => new SemanticScreenNavigation(Target(scope, owner, navigation.Screen), navigation.Trigger) { By = navigation.By, Opens = navigation.Opens })],
            new([.. screen.Layout.Fits], Regions(screen.Layout.Regions)))
        {
            Description = screen.Description,
            Needs = needs.IsDefault ? [] : needs,
            Available = SemanticAvailabilityBinder.Gates(screenSyntax.Available),
            Extensions = [.. (screen.Extensions ?? []).Select(need => new SemanticScreenExtension(System(owner, need.System), need.Reference))],
            Components = [.. (screen.Scene?.Components ?? []).Where(component => component.Extension is not null).Select(component => ExtensionComponent(declaring, owner, component))]
        };
    }

    SemanticScreenComponent ExtensionComponent(SliceSyntax? declaring, string owner, UiSceneComponent component)
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

    SemanticId HandledCommand(SliceSyntax? declaring, string owner, string name)
    {
        var inSlice = declaring is null ? [] : _slices.Values.Where(entry => entry.Slice.Name == declaring.Name).SelectMany(entry => entry.Slice.Commands).Where(command => command.Name == name).ToArray();
        var candidates = inSlice.Length > 0 ? inSlice : [.. _commands.Values.Where(command => command.Name == name)];
        if (candidates.Length == 1) return candidates[0].Id;
        _refusals.Add($"{owner} handles a signal with command '{name}', which {(candidates.Length == 0 ? "no slice declares" : "more than one slice declares")}.");
        return default;
    }

    SemanticScreenAction Action(SliceSyntax? slice, DeclarationScope scope, string owner, UiAction action)
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
            Available = SemanticAvailabilityBinder.Gates(action.Available),
            Then = action.Then is { } then ? Target(scope, owner, then) : null
        };
    }

    SemanticId Target(DeclarationScope scope, string owner, string name)
    {
        if (_declarations!.Resolve(name, scope, candidate => candidate.Screens, screen => screen.Name) is { } resolved &&
            _screenIds.TryGetValue(resolved.Node, out var id))
        {
            return id;
        }

        var scoped = ScreenplayValidator.ScopedScreens(syntax).Where(entry => !entry.InSlice).ToList();
        var pool = scoped.ConvertAll(entry => new Declaration(entry.Screen.Name, entry.Scope));
        if (ReferenceResolver.Resolve(name, scope, pool).Resolved is { } free)
        {
            if (_screenIds.TryGetValue(scoped[pool.IndexOf(free)].Screen, out var freeId)) return freeId;
            _refusals.Add($"{owner} leads to free screen '{name}', which no slice adopts, so it is design-only and not part of the executable model.");
            return default;
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

sealed record FreeScreen(ScreenSyntax Screen, DeclarationScope Scope, SemanticId Id, UiScreen Read, ImmutableArray<SemanticScreenNeed> Needs);
