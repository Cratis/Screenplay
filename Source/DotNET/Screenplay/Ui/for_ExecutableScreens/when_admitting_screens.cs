// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Ui.for_UiContractBuilder.given;

namespace Cratis.Screenplay.Ui.for_ExecutableScreens;

public class when_admitting_screens : a_lending_model_with_screens
{
    ExecutableScreensResult _result;
    ExecutableSemanticModel _model;
    Dictionary<string, SemanticScreen> _screens;
    Dictionary<string, SemanticId> _queries;
    Dictionary<string, SemanticCommand> _commands;

    void Because()
    {
        _result = ExecutableScreens.Admit(Workspace);
        _model = _result.Compilation!.Model;
        var slices = _model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).ToArray();
        _screens = slices.SelectMany(slice => slice.Screens).ToDictionary(screen => screen.Name);
        _queries = slices.SelectMany(slice => slice.Queries).ToDictionary(query => query.Name, query => query.Id);
        _commands = slices.SelectMany(slice => slice.Commands).ToDictionary(command => command.Name);
    }

    SemanticScreen Catalog => _screens["BookCatalog"];

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_esm_v42() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V42);
    [Fact] void should_admit_every_screen() => _screens.Keys.ShouldContainOnly(["AddBook", "BorrowBook", "BookCatalog", "BookDetails", "CatalogCards"]);
    [Fact] void should_identify_a_screen_by_its_address() => Catalog.Id.ShouldEqual(_result.Compilation!.Documents.IdentityCatalog.ResolveSemantic(Address("BookCatalog", "BookCatalog")));
    [Fact] void should_key_the_data_to_the_query() => Catalog.Data[0].Query.ShouldEqual(_queries["AllBooks"]);
    [Fact] void should_keep_the_fields_the_screen_shows() => Catalog.Data[0].Fields.Select(field => (field.Property, field.Label)).ShouldContainOnly([("title", "Title")]);
    [Fact] void should_key_the_actions_to_their_commands() => Catalog.Actions.Select(action => action.Command).ShouldContainOnly([_commands["AddBook"].Id, _commands["BorrowBook"].Id]);
    [Fact] void should_bind_an_input_to_its_command_property() => Catalog.Actions[1].Inputs.Single(input => input.From is not null).ShouldEqual(new SemanticScreenInput(_commands["BorrowBook"].Properties.Single(property => property.Name == "bookId").Id, "item.bookId"));
    [Fact] void should_list_every_input_of_the_command() => Catalog.Actions[1].Inputs.Length.ShouldEqual(2);
    [Fact] void should_key_navigation_to_the_target_screen() => Catalog.Navigation[0].ShouldEqual(new SemanticScreenNavigation(_screens["BookDetails"].Id, "row") { By = "bookId" });
    [Fact] void should_pass_the_value_a_screen_reads_its_data_by() => _screens["BookDetails"].Data[0].By.ShouldEqual("bookId");
    [Fact] void should_keep_the_layout_regions() => Catalog.Layout.Regions.Select(region => region.Kind).ShouldContainOnly(["list"]);

    SemanticAddress Address(string slice, string screen) =>
        SemanticAddress.ForScreen(SemanticAddress.ForSlice(_result.Compilation!.Documents.IdentityCatalog.Application, "Lending", "Catalog", slice), screen);
}
