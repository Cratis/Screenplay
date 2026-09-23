// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_importing_a_name_the_application_declares;

public class and_it_is_used_from_another_module : given.a_folder_with_and_without_the_import
{
    void Establish()
    {
        Application("Catalog.ItemView");
        Write(
            Path.Combine("Catalog", "Items", "ItemView", "ItemView.play"),
            """
            module Catalog
              feature Items
                slice StateView ItemView
                  readmodel ItemView
                    itemId ItemId
                    channel Channel
                  query GetItem => ItemView?
                    by itemId ItemId
            """);
        Write(
            Path.Combine("Shop", "Orders", "PlaceOrder", "PlaceOrder.play"),
            """
            module Shop
              feature Orders
                slice StateChange PlaceOrder
                  command PlaceOrder
                    orderId OrderId identifier
                    itemId ItemId
                    reads ItemView by itemId
                    produces OrderPlaced
                      for orderId
                      orderId = orderId
                      itemId = itemId
                  event OrderPlaced
                    orderId OrderId
                    itemId ItemId
                  specification PlacingAnOrder
                    given readmodel ItemView
                      itemId = "A-1"
                      channel = ""
                    when PlaceOrder
                      orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      itemId = "A-1"
                    then OrderPlaced
                      orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      itemId = "A-1"
            """);
    }

    void Because() => CompileBoth();

    [Fact] void should_report_the_value_outside_the_enum_without_the_import() => ValidationWithoutImport.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownSpecificationEnumMember);
    [Fact] void should_report_the_same_validation_results_with_the_import() => ValidationWithImport.ShouldContainOnly(ValidationWithoutImport);
    [Fact] void should_report_the_import_as_redundant() => _importing.Result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantImport).Message.ShouldEqual("Import 'Catalog.ItemView' names 'ItemView', which this application declares - the import has no effect");
    [Fact] void should_report_the_redundant_import_as_a_warning() => _importing.Result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantImport).Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
