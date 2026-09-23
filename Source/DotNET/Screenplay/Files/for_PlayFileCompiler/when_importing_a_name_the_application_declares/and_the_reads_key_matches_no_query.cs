// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_importing_a_name_the_application_declares;

public class and_the_reads_key_matches_no_query : given.a_folder_with_and_without_the_import
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
                    reads ItemView by orderId
                    produces OrderPlaced
                      for orderId
                  event OrderPlaced
            """);
    }

    void Because() => CompileBoth();

    [Fact] void should_report_the_incompatible_key_without_the_import() => ValidationWithoutImport.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.IncompatibleReadsKey);
    [Fact] void should_report_the_same_validation_results_with_the_import() => ValidationWithImport.ShouldContainOnly(ValidationWithoutImport);
}
