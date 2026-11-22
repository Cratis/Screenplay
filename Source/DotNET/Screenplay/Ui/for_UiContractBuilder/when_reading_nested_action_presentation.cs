// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Comparison;

namespace Cratis.Screenplay.Ui.for_UiContractBuilder;

public class when_reading_nested_action_presentation : Specification
{
    UiContract _contract;

    void Because()
    {
        var workspace = ComparedModel.FromSources("Work", new Dictionary<string, string> { ["application.play"] = Source() }).Workspace;
        _contract = UiContractBuilder.For(workspace);
    }

    [Fact] void should_keep_adopted_free_screen_presentation() => _contract.Screens.Single(screen => screen.Name == "Shared").Scene!.Actions!.Single().Presentation["placement"].ShouldEqual("row-menu");
    [Fact] void should_identify_the_adopting_slice() => _contract.Screens.Single(screen => screen.Name == "Shared").Slice.ShouldEqual("Work.Items.List");
    [Fact] void should_keep_nested_section_presentation_in_declaration_order() => _contract.Screens.Single(screen => screen.Name == "Local").Scene!.Actions!.Select(action => action.Presentation["placement"]).ShouldContainOnly(["row-menu", "future-menu"]);
    [Fact] void should_distinguish_hinted_and_unhinted_occurrences_of_the_same_command() => _contract.Screens.Single(screen => screen.Name == "Local").Scene!.Actions!.Select(action => action.ActionIndex).ShouldContainOnly([0, 2]);
    [Fact] void should_keep_unknown_keys() => _contract.Screens.Single(screen => screen.Name == "Shared").Scene!.Actions!.Single().Presentation["extension"].ShouldEqual("future");
    [Fact] void should_omit_the_unhinted_action_from_scene_entries() => _contract.Screens.Single(screen => screen.Name == "Local").Actions.Count.ShouldEqual(3);

    static string Source([CallerFilePath] string path = "") => File.ReadAllText(Path.Combine(Path.GetDirectoryName(path)!, "../../Parsing/for_ScreenActionPresentation/action-presentation.play"));
}
