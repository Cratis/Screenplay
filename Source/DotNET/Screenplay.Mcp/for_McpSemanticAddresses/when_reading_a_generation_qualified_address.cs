// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpSemanticAddresses;

public class when_reading_a_generation_qualified_address : Specification
{
    SemanticAddress _expected = null!;
    SemanticAddress _actual = null!;

    void Because()
    {
        var app = ApplicationIdentity.Create("Projects");
        var slice = SemanticAddress.ForSlice(app, "Projects", "Registration", "RegisterProject");
        _expected = SemanticAddress.ForEventProperty(SemanticAddress.ForEventContract(slice, "Registered"), new(2), "name");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(McpSemanticAddresses.Describe(_expected), McpJson.Options);
        using var document = JsonDocument.Parse(bytes);
        _actual = McpSemanticAddresses.Read(document.RootElement);
    }

    [Fact] void should_preserve_the_generation() => _actual.ShouldEqual(_expected);
    [Fact] void should_refuse_an_invalid_generation()
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(McpSemanticAddresses.Describe(_expected), McpJson.Options);
        using var document = JsonDocument.Parse(System.Text.Encoding.UTF8.GetString(bytes).Replace("\"key\":\"2\"", "\"key\":\"0\"", StringComparison.Ordinal));
        Catch.Exception(() => McpSemanticAddresses.Read(document.RootElement)).ShouldBeOfExactType<McpFailure>();
    }
}
