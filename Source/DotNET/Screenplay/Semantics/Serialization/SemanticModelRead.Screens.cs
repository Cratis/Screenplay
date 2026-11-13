// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep version-aware parsing delegates clear.
internal static partial class SemanticModelRead
{
    internal static SemanticScreen Screen(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "screen");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        string? title = null;
        string? description = null;
        ImmutableArray<SemanticScreenData> data = default;
        ImmutableArray<SemanticScreenAction> actions = default;
        ImmutableArray<SemanticScreenNavigation> navigation = default;
        SemanticScreenLayout? layout = null;
        ImmutableArray<SemanticScreenExtension> extensions = [];
        ImmutableArray<SemanticScreenComponent> components = [];
        ImmutableArray<SemanticAvailabilityRequirement> available = [];
        while (NextProperty(ref reader, seen, "screen") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "title": title = String(ref reader, property); break;
                case "description": description = String(ref reader, property); break;
                case "data": data = Array(ref reader, ScreenData, property); break;
                case "actions": actions = Array(ref reader, ScreenAction, property); break;
                case "navigation": navigation = Array(ref reader, ScreenNavigation, property); break;
                case "layout": RequiredRead(ref reader, property); layout = ScreenLayout(ref reader); break;
                case "extensions" when schemaVersion >= 52: extensions = Array(ref reader, ScreenExtension, property); break;
                case "available" when schemaVersion >= 53: available = Array(ref reader, AvailabilityRequirement, property); break;
                case "components" when schemaVersion >= 52: components = Array(ref reader, ScreenComponent, property); break;
                default: throw Unknown(property, "screen");
            }
        }

        Required(id.IsSet && name is not null && title is not null && !data.IsDefault && !actions.IsDefault && !navigation.IsDefault && layout is not null, "screen");
        return new(id, name!, title!, data, actions, navigation, layout!) { Description = description, Extensions = extensions, Components = components, Available = available };
    }

    static SemanticScreenExtension ScreenExtension(ref Utf8JsonReader reader)
    {
        Object(ref reader, "screen extension");
        var seen = NewSeen();
        SemanticId system = default;
        string? extensionReference = null;
        while (NextProperty(ref reader, seen, "screen extension") is { } property)
        {
            switch (property)
            {
                case "system": system = SemanticId.Parse(String(ref reader, property)); break;
                case "reference": extensionReference = String(ref reader, property); break;
                default: throw Unknown(property, "screen extension");
            }
        }

        Required(system.IsSet && extensionReference is not null, "screen extension");
        return new(system, extensionReference!);
    }

    static SemanticScreenComponent ScreenComponent(ref Utf8JsonReader reader)
    {
        Object(ref reader, "screen component");
        var seen = NewSeen();
        string? name = null;
        SemanticId system = default;
        string? component = null;
        ImmutableArray<string> properties = default;
        ImmutableArray<SemanticScreenSignalHandler> handles = default;
        while (NextProperty(ref reader, seen, "screen component") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "system": system = SemanticId.Parse(String(ref reader, property)); break;
                case "component": component = String(ref reader, property); break;
                case "properties": properties = StringArray(ref reader, property); break;
                case "handles": handles = Array(ref reader, ScreenSignalHandler, property); break;
                default: throw Unknown(property, "screen component");
            }
        }

        Required(name is not null && system.IsSet && component is not null && !properties.IsDefault && !handles.IsDefault, "screen component");
        return new(name!, system, component!, properties, handles);
    }

    static SemanticScreenSignalHandler ScreenSignalHandler(ref Utf8JsonReader reader)
    {
        Object(ref reader, "screen signal handler");
        var seen = NewSeen();
        string? signal = null;
        ImmutableArray<SemanticId> commands = default;
        while (NextProperty(ref reader, seen, "screen signal handler") is { } property)
        {
            switch (property)
            {
                case "signal": signal = String(ref reader, property); break;
                case "commands": commands = [.. StringArray(ref reader, property).Select(SemanticId.Parse)]; break;
                default: throw Unknown(property, "screen signal handler");
            }
        }

        Required(signal is not null && !commands.IsDefault, "screen signal handler");
        return new(signal!, commands);
    }

    static SemanticScreenData ScreenData(ref Utf8JsonReader reader)
    {
        Object(ref reader, "screen data");
        var seen = NewSeen();
        SemanticId query = default;
        string? by = null;
        ImmutableArray<SemanticScreenField> fields = default;
        while (NextProperty(ref reader, seen, "screen data") is { } property)
        {
            switch (property)
            {
                case "query": query = SemanticId.Parse(String(ref reader, property)); break;
                case "by": by = String(ref reader, property); break;
                case "fields": fields = Array(ref reader, ScreenField, property); break;
                default: throw Unknown(property, "screen data");
            }
        }

        Required(query.IsSet && !fields.IsDefault, "screen data");
        return new(query, fields) { By = by };
    }

    static SemanticScreenField ScreenField(ref Utf8JsonReader reader)
    {
        Object(ref reader, "screen field");
        var seen = NewSeen();
        string? name = null;
        string? label = null;
        while (NextProperty(ref reader, seen, "screen field") is { } property)
        {
            switch (property)
            {
                case "path": name = String(ref reader, property); break;
                case "label": label = String(ref reader, property); break;
                default: throw Unknown(property, "screen field");
            }
        }

        Required(name is not null, "screen field");
        return new(name!, label);
    }

    static SemanticScreenAction ScreenAction(ref Utf8JsonReader reader)
    {
        Object(ref reader, "screen action");
        var seen = NewSeen();
        SemanticId command = default;
        string? label = null;
        string? subject = null;
        string? when = null;
        SemanticId? then = null;
        ImmutableArray<SemanticScreenInput> inputs = default;
        while (NextProperty(ref reader, seen, "screen action") is { } property)
        {
            switch (property)
            {
                case "command": command = SemanticId.Parse(String(ref reader, property)); break;
                case "label": label = String(ref reader, property); break;
                case "subject": subject = String(ref reader, property); break;
                case "inputs": inputs = Array(ref reader, ScreenInput, property); break;
                case "when": when = String(ref reader, property); break;
                case "then": then = SemanticId.Parse(String(ref reader, property)); break;
                default: throw Unknown(property, "screen action");
            }
        }

        Required(command.IsSet && label is not null && subject is not null && !inputs.IsDefault, "screen action");
        return new(command, label!, subject!, inputs) { When = when, Then = then };
    }

    static SemanticScreenInput ScreenInput(ref Utf8JsonReader reader)
    {
        Object(ref reader, "screen input");
        var seen = NewSeen();
        SemanticId target = default;
        string? from = null;
        while (NextProperty(ref reader, seen, "screen input") is { } property)
        {
            switch (property)
            {
                case "property": target = SemanticId.Parse(String(ref reader, property)); break;
                case "from": from = String(ref reader, property); break;
                default: throw Unknown(property, "screen input");
            }
        }

        Required(target.IsSet, "screen input");
        return new(target, from);
    }

    static SemanticScreenNavigation ScreenNavigation(ref Utf8JsonReader reader)
    {
        Object(ref reader, "screen navigation");
        var seen = NewSeen();
        SemanticId screen = default;
        string? trigger = null;
        string? by = null;
        string? opens = null;
        while (NextProperty(ref reader, seen, "screen navigation") is { } property)
        {
            switch (property)
            {
                case "screen": screen = SemanticId.Parse(String(ref reader, property)); break;
                case "trigger": trigger = String(ref reader, property); break;
                case "by": by = String(ref reader, property); break;
                case "opens": opens = String(ref reader, property); break;
                default: throw Unknown(property, "screen navigation");
            }
        }

        Required(screen.IsSet && trigger is not null, "screen navigation");
        return new(screen, trigger!) { By = by, Opens = opens };
    }

    static SemanticScreenLayout ScreenLayout(ref Utf8JsonReader reader)
    {
        Object(ref reader, "screen layout");
        var seen = NewSeen();
        ImmutableArray<string> fits = default;
        ImmutableArray<SemanticScreenRegion> regions = default;
        while (NextProperty(ref reader, seen, "screen layout") is { } property)
        {
            switch (property)
            {
                case "fits": fits = StringArray(ref reader, property); break;
                case "regions": regions = Array(ref reader, ScreenRegion, property); break;
                default: throw Unknown(property, "screen layout");
            }
        }

        Required(!fits.IsDefault && !regions.IsDefault, "screen layout");
        return new(fits, regions);
    }

    static SemanticScreenRegion ScreenRegion(ref Utf8JsonReader reader)
    {
        Object(ref reader, "screen region");
        var seen = NewSeen();
        string? kind = null;
        string? name = null;
        string? shows = null;
        string? title = null;
        ImmutableArray<string> actions = [];
        ImmutableArray<SemanticScreenRegion> regions = default;
        while (NextProperty(ref reader, seen, "screen region") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "name": name = String(ref reader, property); break;
                case "shows": shows = String(ref reader, property); break;
                case "title": title = String(ref reader, property); break;
                case "actions": actions = StringArray(ref reader, property); break;
                case "regions": regions = Array(ref reader, ScreenRegion, property); break;
                default: throw Unknown(property, "screen region");
            }
        }

        Required(kind is not null && !regions.IsDefault, "screen region");
        return new(kind!, regions) { Name = name, Shows = shows, Title = title, Actions = actions };
    }
}
