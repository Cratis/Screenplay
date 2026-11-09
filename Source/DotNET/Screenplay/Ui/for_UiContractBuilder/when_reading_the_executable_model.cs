// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Ui.for_UiContractBuilder;

public class when_reading_the_executable_model : given.a_contract
{
    UiContract _read;

    UiScreen Read(string name) => _read.Screens.Single(screen => screen.Name == name);

    void Because()
    {
        var model = ExecutableScreens.Admit(Workspace).Compilation!.Model;
        _read = UiContractBuilder.For(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(model)));
    }

    [Fact] void should_carry_the_model_revision() => _read.ModelRevision.ShouldNotBeNull();
    [Fact] void should_name_the_query_and_read_model() => (Read("BookCatalog").Data[0].Query, Read("BookCatalog").Data[0].ReadModel).ShouldEqual(("AllBooks", "CatalogEntry"));
    [Fact] void should_route_the_data() => Read("BookCatalog").Data[0].Route.ShouldEqual(Screen("BookCatalog").Data[0].Route);
    [Fact] void should_type_the_command_inputs() => Read("BookCatalog").Actions[1].Inputs.Select(input => (input.Name, input.Type, input.From)).ShouldContainOnly([("bookId", "BookId", "item.bookId"), ("memberId", "MemberId", null)]);
    [Fact] void should_name_the_target_screen() => Read("BookCatalog").Navigation[0].Screen.ShouldEqual("BookDetails");
    [Fact] void should_leave_out_the_scene_layer() => _read.Screens.All(screen => screen.Scene is null && screen.Layout.Template is null).ShouldBeTrue();
}
