// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler.given;

/// <summary>
/// A catalog with a list template offering slots by role and two action outlets, State Change slices that fit places or run,
/// a form with a picker and a list screen that shows the selected row's details in its routed slot (Cratis/Screenplay#1178).
/// </summary>
public class a_placement_document : Specification
{
    /// <summary>
    /// The valid document; the placeholders <c>{{feature}}</c>, <c>{{form}}</c> and <c>{{screen}}</c> take extra lines.
    /// </summary>
    protected const string Template =
        """
        concept CategoryId : Uuid
        module Catalog
          screen template ListPage
            category Lists
            header
            main
            detail pane routed by sku optional
            activity panel
            outlet toolbar of actions
              default refresh title "Refresh" runs refresh
            outlet rowActions of actions for item

          form DraftProductForm for DraftProduct
            field category
              choose from query AllCategories show name
            field price
        {{form}}
          feature Products
            outlet toolbar overrides
              button add title "Add product" slice DraftProduct emphasis primary opens dialog
              button price title "Set price" slice SetProductPrice for item opens in slot detail
        {{feature}}
            outlet rowActions extends
              button withdraw title "Withdraw" slice WithdrawProduct
                runs
                enabled when item.status == "onSale"
                confirm "Withdraw this product?"

            slice StateChange DraftProduct
              command DraftProduct
                sku String identifier
                category CategoryId
                price Decimal
                produces ProductDrafted
                  for sku
              event ProductDrafted
                sku String
              screen DraftProduct
                fits dialog, drawer, pane
                action DraftProduct for screen

            slice StateChange SetProductPrice
              command SetProductPrice
                sku String identifier
                price Decimal
                produces PriceSet
                  for sku
              event PriceSet
                sku String
              screen SetProductPrice
                fits drawer, pane, dialog
                action SetProductPrice for screen

            slice StateChange WithdrawProduct
              command WithdrawProduct
                sku String identifier
                produces ProductWithdrawn
                  for sku
              event ProductWithdrawn
                sku String

            slice StateView Categories
              readmodel CategoryRow
                id CategoryId key
                name String
              query AllCategories => CategoryRow[]

            slice StateView ProductCatalog
              readmodel ProductRow
                sku String
                status String
              query AllProducts => ProductRow[]
              screen ProductCatalog
                template ListPage
                  main
                    table products
                      column sku
                      column status
                  detail
                    show ProductDetails by sku from products.selection
        {{screen}}

            slice StateView ProductDetails
              readmodel ProductDetail
                sku String
              query ProductBySku => ProductDetail
                by sku String
              screen ProductDetails
                data ProductDetail via query ProductBySku by sku
        """;

    /// <summary>
    /// Gets the document with extra lines in place of the placeholders.
    /// </summary>
    /// <param name="feature">Lines added to the feature's toolbar override.</param>
    /// <param name="form">Lines added after the form.</param>
    /// <param name="screen">Lines added to the list screen.</param>
    /// <returns>The source text.</returns>
    protected static string Source(string feature = "", string form = "", string screen = "") =>
        Template.Replace("{{feature}}\n", feature, StringComparison.Ordinal)
            .Replace("{{form}}\n", form, StringComparison.Ordinal)
            .Replace("{{screen}}\n", screen, StringComparison.Ordinal);

    /// <summary>
    /// Gets the codes of the warnings and errors compiling a document reports.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <returns>The codes.</returns>
    protected static IEnumerable<string> Codes(string source) =>
        new ScreenplayCompiler().Compile(source).Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).Select(diagnostic => diagnostic.Code);
}
