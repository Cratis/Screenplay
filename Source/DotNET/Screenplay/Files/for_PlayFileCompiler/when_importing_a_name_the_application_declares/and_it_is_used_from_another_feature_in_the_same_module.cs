// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_importing_a_name_the_application_declares;

public class and_it_is_used_from_another_feature_in_the_same_module : given.a_folder_with_and_without_the_import
{
    void Establish()
    {
        Application("Shop.ItemView");
        Write(
            Path.Combine("Shop", "Items", "ItemView", "ItemView.play"),
            """
            module Shop
              feature Items
                slice StateView ItemView
                  readmodel ItemView
                    itemId ItemId
                    channel Channel
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
                    produces OrderPlaced
                      for orderId
                      itemId = itemId
                  event OrderPlaced
                    itemId ItemId
                  specification PlacingAnOrder
                    given readmodel ItemView
                      itemId = "A-1"
                      channel = ""
                    when PlaceOrder
                      orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      itemId = "A-1"
                    then OrderPlaced
                      itemId = "A-1"
            """);
    }

    void Because() => CompileBoth();

    [Fact] void should_report_the_value_outside_the_enum_without_the_import() => ValidationWithoutImport.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownSpecificationEnumMember);
    [Fact] void should_report_the_same_validation_results_with_the_import() => ValidationWithImport.ShouldContainOnly(ValidationWithoutImport);
    [Fact] void should_report_the_import_as_redundant() => _importing.Result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantImport).ShouldEqual(1);
}
