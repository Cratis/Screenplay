// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FormFieldChoices;

public class when_inferring_a_picker : Specification
{
    const string Source =
        """
        concept CategoryId : Uuid
        concept SupplierId : Uuid
        module Catalog
          form DraftProductForm for DraftProduct
            field category
            field supplier
            field name
            field shelf
              choose from query AllShelves show code
          feature Products
            slice StateChange DraftProduct
              command DraftProduct
                sku String identifier
                category CategoryId
                supplier SupplierId
                name String
                shelf String
                produces ProductDrafted
                  for sku
              event ProductDrafted
                sku String
              screen DraftProduct
                action DraftProduct
            slice StateView Categories
              readmodel CategoryRow
                id CategoryId key
                code String
                name String
              query AllCategories => CategoryRow[]
            slice StateView Suppliers
              readmodel SupplierRow
                id SupplierId key
                code String
              query AllSuppliers => SupplierRow[]
              query PreferredSuppliers => SupplierRow[]
            slice StateView Shelves
              readmodel ShelfRow
                code String
              query AllShelves => ShelfRow[]
        """;

    ApplicationSyntax _application;
    FormSyntax _form;

    void Establish()
    {
        _application = new ScreenplayCompiler().Compile(Source).Value!;
        _form = _application.Modules.Single().Forms!.Single();
    }

    FormFieldChoicesSyntax? Choices(string property) => FormFieldChoices.Resolve(_application, _form, _form.Fields.Single(field => field.Property == property));

    [Fact] void should_infer_the_list_query_of_the_read_model_the_concept_keys() => Choices("category")!.Query.ShouldEqual("AllCategories");
    [Fact] void should_show_the_name_field() => Choices("category")!.Show.ShouldEqual("name");
    [Fact] void should_not_infer_when_two_list_queries_return_the_read_model() => Choices("supplier").ShouldBeNull();
    [Fact] void should_not_infer_for_a_value_no_read_model_is_keyed_by() => Choices("name").ShouldBeNull();
    [Fact] void should_keep_the_written_choices() => (Choices("shelf")!.Query, Choices("shelf")!.Show).ShouldEqual(("AllShelves", "code"));
}
