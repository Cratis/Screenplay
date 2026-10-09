// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_processing_purposes : given.a_connection
{
    McpSnapshot _snapshot = null!;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "model.play"), """
        purpose Billing
          description "Issue invoices"
          basis contract
        module M
          purpose Billing
          feature F
            slice StateChange S
              purpose Billing
        """);

    void Because() => _snapshot = new(Root.Read());

    [Fact] void should_expose_the_declared_basis() => Details("Billing", "Purpose").GetProperty("processingPurpose").GetProperty("Basis").GetString().ShouldEqual("contract");
    [Fact] void should_expose_module_references() => Details("M", "Module").GetProperty("purposes").GetArrayLength().ShouldEqual(1);
    [Fact] void should_expose_slice_references() => Details("M.F.S", "Slice").GetProperty("purposes").GetArrayLength().ShouldEqual(1);
    [Fact] void should_find_purpose_references() => _snapshot.Index.References.Count(reference => reference.Kinds.Contains("Purpose")).ShouldEqual(2);

    JsonElement Details(string address, string kind) => JsonSerializer.SerializeToElement(McpDeclarationDetails.Read(_snapshot, JsonSerializer.SerializeToElement(new { address, kind, view = "summary" }))).GetProperty("details");
}
