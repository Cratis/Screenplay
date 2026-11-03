// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Languages;

namespace Cratis.Screenplay.Mcp.for_McpAttachmentContents;

public class when_configuration_changes_within_a_request : given.an_attachment_request
{
    readonly List<string> _values = ["Original"];
    ScreenplayLanguageRegistry _registry = null!;
    HashSet<string> _languages = null!;
    Dictionary<string, TriggerDefinition> _triggers = null!;
    bool _languageInvalidated;
    bool _triggerInvalidated;
    bool _shapeInvalidated;
    bool _languageRestored;
    bool _triggerRestored;
    bool _shapeRestored;

    void Establish()
    {
        _registry = new(triggers: [new("SpecTrigger", _values)]);
        _languages = (HashSet<string>)_registry.InlineLanguages;
        _triggers = (Dictionary<string, TriggerDefinition>)_registry.Triggers;
    }

    void Because()
    {
        (_languageInvalidated, _languageRestored) = CheckConfigurationChange(() => _languages.Add("isolateddsl"), () => _languages.Remove("isolateddsl"));
        var original = _values[0];
        (_triggerInvalidated, _triggerRestored) = CheckConfigurationChange(() => _values[0] = "Changed", () => _values[0] = original);
        ChangeTriggerShape();
    }

    [Fact]
    void should_invalidate_when_an_inline_language_is_added()
    {
        _languageInvalidated.ShouldBeTrue();
        _languageRestored.ShouldBeTrue();
    }

    [Fact]
    void should_invalidate_when_a_trigger_value_changes_in_place()
    {
        _triggerInvalidated.ShouldBeTrue();
        _triggerRestored.ShouldBeTrue();
    }

    [Fact]
    void should_distinguish_unspecified_trigger_values_from_an_explicitly_empty_shape()
    {
        _shapeInvalidated.ShouldBeTrue();
        _shapeRestored.ShouldBeTrue();
    }

    (bool Invalidated, bool Restored) CheckConfigurationChange(Action change, Action restore)
    {
        var request = new McpAttachmentContents.Request(_registry);
        var first = request.References(Documents);
        bool invalidated;
        try
        {
            change();
            invalidated = !request.References(Documents).Equals(first);
        }
        finally
        {
            restore();
        }
        return (invalidated, request.References(Documents).Equals(first));
    }

    void ChangeTriggerShape()
    {
        var request = new McpAttachmentContents.Request(_registry);
        var first = request.References(Documents);
        var original = _triggers["SpecTrigger"];
        try
        {
            _triggers["SpecTrigger"] = new("SpecTrigger", null);
            var unspecified = request.References(Documents);
            _triggers["SpecTrigger"] = new("SpecTrigger", []);
            var empty = request.References(Documents);
            _shapeInvalidated = !unspecified.Equals(first) && !empty.Equals(first) && !empty.Equals(unspecified);
        }
        finally
        {
            _triggers["SpecTrigger"] = original;
        }
        _shapeRestored = request.References(Documents).Equals(first);
    }
}
