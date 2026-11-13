// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Writes the ESM v42 screens of a slice. Screens keep the order of their data, actions, navigation and regions as declared,
/// because that order is what a screen shows.
/// </summary>
public static partial class SemanticModelCanonicalJson
{
    static void WriteScreens(Utf8JsonWriter writer, SemanticSlice slice, SemanticVersion version)
    {
        if (slice.Screens.IsDefaultOrEmpty) return;
        if (version.Major < 42) throw new InvalidSemanticContract("Screens require ESM v42 or later.");
        if (version.Major < 52 && slice.Screens.Any(screen => !screen.Components.IsDefaultOrEmpty || !screen.Extensions.IsDefaultOrEmpty)) throw new InvalidSemanticContract("Extension components on screens require ESM v52 or later.");
        WriteArray(writer, "screens", slice.Screens.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), (output, screen) => WriteScreen(output, screen, version));
    }

    static void WriteScreen(Utf8JsonWriter writer, SemanticScreen screen, SemanticVersion version)
    {
        writer.WriteStartObject();
        WriteId(writer, screen.Id);
        CanonicalJson.WriteString(writer, "name", screen.Name);
        CanonicalJson.WriteString(writer, "title", screen.Title);
        if (screen.Description is { } description) CanonicalJson.WriteString(writer, "description", description);
        WriteArray(writer, "data", screen.Data, WriteScreenData);
        WriteArray(writer, "actions", screen.Actions, WriteScreenAction);
        WriteArray(writer, "navigation", screen.Navigation, WriteScreenNavigation);
        if (!screen.Extensions.IsDefaultOrEmpty) WriteArray(writer, "extensions", screen.Extensions, WriteScreenExtension);
        if (!screen.Components.IsDefaultOrEmpty) WriteArray(writer, "components", screen.Components, WriteScreenComponent);
        WriteAvailable(writer, screen.Available, version);
        writer.WritePropertyName("layout");
        writer.WriteStartObject();
        WriteStringArray(writer, "fits", screen.Layout.Fits);
        WriteArray(writer, "regions", screen.Layout.Regions, WriteScreenRegion);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    static void WriteScreenExtension(Utf8JsonWriter writer, SemanticScreenExtension extension)
    {
        writer.WriteStartObject();
        writer.WriteString("system", extension.System.ToString());
        CanonicalJson.WriteString(writer, "reference", extension.Reference);
        writer.WriteEndObject();
    }

    static void WriteScreenComponent(Utf8JsonWriter writer, SemanticScreenComponent component)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", component.Name);
        writer.WriteString("system", component.System.ToString());
        CanonicalJson.WriteString(writer, "component", component.Component);
        WriteStringArray(writer, "properties", component.Properties);
        WriteArray(writer, "handles", component.Handles, WriteScreenSignalHandler);
        writer.WriteEndObject();
    }

    static void WriteScreenSignalHandler(Utf8JsonWriter writer, SemanticScreenSignalHandler handler)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "signal", handler.Signal);
        WriteStringArray(writer, "commands", [.. handler.Commands.Select(command => command.ToString())]);
        writer.WriteEndObject();
    }

    static void WriteScreenData(Utf8JsonWriter writer, SemanticScreenData data)
    {
        writer.WriteStartObject();
        writer.WriteString("query", data.Query.ToString());
        if (data.By is { } by) CanonicalJson.WriteString(writer, "by", by);
        WriteArray(writer, "fields", data.Fields, WriteScreenField);
        writer.WriteEndObject();
    }

    static void WriteScreenField(Utf8JsonWriter writer, SemanticScreenField field)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "path", field.Property);
        if (field.Label is { } label) CanonicalJson.WriteString(writer, "label", label);
        writer.WriteEndObject();
    }

    static void WriteScreenAction(Utf8JsonWriter writer, SemanticScreenAction action)
    {
        writer.WriteStartObject();
        writer.WriteString("command", action.Command.ToString());
        CanonicalJson.WriteString(writer, "label", action.Label);
        CanonicalJson.WriteString(writer, "subject", action.Subject);
        WriteArray(writer, "inputs", action.Inputs, WriteScreenInput);
        if (action.When is { } when) CanonicalJson.WriteString(writer, "when", when);
        if (action.Then is { } then) writer.WriteString("then", then.ToString());
        writer.WriteEndObject();
    }

    static void WriteScreenInput(Utf8JsonWriter writer, SemanticScreenInput input)
    {
        writer.WriteStartObject();
        writer.WriteString("property", input.Property.ToString());
        if (input.From is { } from) CanonicalJson.WriteString(writer, "from", from);
        writer.WriteEndObject();
    }

    static void WriteScreenNavigation(Utf8JsonWriter writer, SemanticScreenNavigation navigation)
    {
        writer.WriteStartObject();
        writer.WriteString("screen", navigation.Screen.ToString());
        CanonicalJson.WriteString(writer, "trigger", navigation.Trigger);
        if (navigation.By is { } by) CanonicalJson.WriteString(writer, "by", by);
        if (navigation.Opens is { } opens) CanonicalJson.WriteString(writer, "opens", opens);
        writer.WriteEndObject();
    }

    static void WriteScreenRegion(Utf8JsonWriter writer, SemanticScreenRegion region)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "kind", region.Kind);
        if (region.Name is { } name) CanonicalJson.WriteString(writer, "name", name);
        if (region.Shows is { } shows) CanonicalJson.WriteString(writer, "shows", shows);
        if (region.Title is { } title) CanonicalJson.WriteString(writer, "title", title);
        if (!region.Actions.IsDefaultOrEmpty) WriteStringArray(writer, "actions", region.Actions);
        WriteArray(writer, "regions", region.Regions, WriteScreenRegion);
        writer.WriteEndObject();
    }
}
