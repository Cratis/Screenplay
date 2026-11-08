// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Ui.for_UiContractBuilder;

public class when_reading_the_scene_layer : given.a_contract
{
    UiSceneComponent Component => Screen("CatalogCards").Scene!.Components.Single();

    [Fact] void should_leave_out_the_layer_for_a_screen_without_scene_vocabulary() => Screen("BookCatalog").Scene.ShouldBeNull();
    [Fact] void should_name_the_component_type() => Component.Component.ShouldEqual("Cratis.Blueprint.Default.dataList");
    [Fact] void should_keep_the_stable_identity() => Component.StableId.ShouldEqual("catalogList");
    [Fact] void should_spell_a_query_binding() => Component.Properties["rows"]!.GetValue<string>().ShouldEqual("query AllBooks");
    [Fact] void should_keep_a_text_value_as_text() => Component.Properties["variant"]!.GetValue<string>().ShouldEqual("cards");
    [Fact] void should_show_the_component_as_a_neutral_region() => Screen("CatalogCards").Layout.Regions.Single().Kind.ShouldEqual("component");
    [Fact] void should_list_the_template_slots() => Screen("AddBook").Scene!.Templates.Single().Slots.ShouldContainOnly(["body", "actions"]);
}
