// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Extensions.for_ExtensionCatalog;

public class when_resolving_a_reference
{
    [Fact] void should_resolve_a_short_name_to_the_default_registry() => ExtensionCatalog.Resolve("cratis/stripe").ShouldEqual("ghcr.io/cratis/extension-stripe");
    [Fact] void should_keep_a_full_oci_reference() => ExtensionCatalog.Resolve("ghcr.io/acme/extension-ledger").ShouldEqual("ghcr.io/acme/extension-ledger");
    [Fact] void should_keep_an_npm_reference() => ExtensionCatalog.Resolve("npm:@acme/charts").ShouldEqual("npm:@acme/charts");
}
