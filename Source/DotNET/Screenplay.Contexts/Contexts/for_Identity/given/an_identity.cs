// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_Identity.given;

public class an_identity : Specification
{
    protected Identity _identity;

    void Establish() => _identity = new(
        "e8b0c5f2-1f9a-4c8d-9f3b-2b6f1c0d5a71",
        "Ada Lovelace",
        "ada",
        true,
        ["Accountant", "InvoiceManager"],
        [
            new Claim("department", "Finance"),
            new Claim("scope", "invoices.read"),
            new Claim("scope", "invoices.write")
        ]);
}
