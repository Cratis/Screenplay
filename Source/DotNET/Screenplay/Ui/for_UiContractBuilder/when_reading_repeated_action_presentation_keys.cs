// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Ui.for_UiContractBuilder;

public class when_reading_repeated_action_presentation_keys : Specification
{
    UiScene _scene;

    void Because()
    {
        var reader = new UiSceneReader();
        reader.Action(new ScreenActionSyntax("Retry", null, null, SourceLocation.Start)
        {
            Subject = ScreenActionSubjectKind.Item,
            Presentation = [new("placement", "row-menu", SourceLocation.Start), new("placement", "future-menu", SourceLocation.Start)]
        }, 0);
        _scene = reader.Build()!;
    }

    [Fact] void should_keep_the_last_value_when_reading_an_unvalidated_typed_action() => _scene.Actions!.Single().Presentation["placement"].ShouldEqual("future-menu");
    [Fact] void should_keep_one_value_per_key() => _scene.Actions!.Single().Presentation.Count.ShouldEqual(1);
}
