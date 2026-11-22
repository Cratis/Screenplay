// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Parsing.for_ScreenActionPresentation;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Ui.for_UiContractBuilder;

public class when_reading_action_presentation : Specification
{
    UiScreen _source;
    UiScreen _executable;
    UiScreen _unhinted;

    void Because()
    {
        var text = when_parsing_action_presentation.Source;
        var workspace = ComparedModel.FromSources("Work", new Dictionary<string, string> { ["application.play"] = text }).Workspace;
        _source = UiContractBuilder.For(workspace).Screens.Single();
        _executable = UiContractBuilder.For(ExecutableScreens.Admit(workspace).Compilation!.Model).Screens.Single();
        var plain = text.Replace("          presentation style \"quiet\"\n          presentation placement \"row-menu\"\n", string.Empty, StringComparison.Ordinal);
        _unhinted = UiContractBuilder.For(ComparedModel.FromSources("Work", new Dictionary<string, string> { ["application.play"] = plain }).Workspace).Screens.Single();
    }

    [Fact] void should_expose_the_hint_only_in_the_scene_layer() => _source.Scene!.Actions!.Single().Presentation["placement"].ShouldEqual("row-menu");
    [Fact] void should_identify_the_command() => _source.Scene!.Actions!.Single().Command.ShouldEqual("Retry");
    [Fact] void should_sort_presentation_keys() => _source.Scene!.Actions!.Single().Presentation.Keys.ShouldContainOnly(["placement", "style"]);
    [Fact] void should_keep_neutral_actions_equal_after_esm_round_trip() => JsonSerializer.Serialize(_source.Actions).ShouldEqual(JsonSerializer.Serialize(_executable.Actions));
    [Fact] void should_not_admit_the_hint_to_the_esm() => _executable.Scene.ShouldBeNull();
    [Fact] void should_not_add_a_scene_layer_to_unhinted_actions() => _unhinted.Scene.ShouldBeNull();
    [Fact] void should_not_change_the_neutral_contract() => JsonSerializer.Serialize(_source with { Scene = null }).ShouldEqual(JsonSerializer.Serialize(_unhinted));
}
