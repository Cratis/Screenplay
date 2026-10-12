// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_following_multi_source_property_dependencies : given.a_connection
{
    JsonElement _outgoing;
    JsonElement _incoming;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Projects
              feature Registration
                slice StateView Lookup
                  event Counted
                    first String
                    second String
                  readmodel Total
                    count String
                  projection Totals => Total
                    no automap
                    from Counted
                      count = `${first}${second}`
            """);
        Initialize();
    }

    void Because()
    {
        _outgoing = Read("outgoing");
        _incoming = Read("incoming");
    }

    [Fact] void should_keep_the_mapping_target_in_the_outgoing_view() => _outgoing.GetProperty("page").GetProperty("items").EnumerateArray().SelectMany(item => item.GetProperty("targets").EnumerateArray()).Select(target => target.GetProperty("address").GetString()).ShouldContain("Projects.Registration.Lookup.Total.count");
    [Fact] void should_keep_the_mapping_source_in_the_incoming_view() => _incoming.GetProperty("page").GetProperty("items").EnumerateArray().Select(item => item.GetProperty("reference").GetProperty("role").GetString()).ShouldContain("mapping-source");

    JsonElement Read(string direction) => Call("dependencies", new { address = "Projects.Registration.Lookup.Counted.first", kind = "Property", direction }).GetProperty("result").GetProperty("structuredContent");
}
