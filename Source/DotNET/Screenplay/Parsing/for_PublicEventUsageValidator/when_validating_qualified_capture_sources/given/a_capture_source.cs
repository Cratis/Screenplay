// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing.for_PublicEventUsageValidator.when_validating_qualified_capture_sources.given;

public class a_capture_source : Specification
{
    protected static string Source(string name) => $$"""
        concept ProductId : Uuid
        module Products
          feature Catalog
            slice Translate Facts
              direction inbound
              event ProductPublished from "catalog"
                productId ProductId
              event ProductReceived
                productId ProductId
              capture ProductsFeed
                source events
                  from {{name}}
                key productId
                append ProductReceived
                  productId = $.productId
        """;
}
