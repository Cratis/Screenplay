// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_reopening_drifted_unbound_container_sources : given.an_unbound_persisted_notes_workspace
{
    [Theory]
    [InlineData("unparsed", "IdentityReconciliationRequired")]
    [InlineData("rename", "IdentityReconciliationRequired")]
    [InlineData("binding", "IdentityReconciliationRequired")]
    [InlineData("mapping", "IdentityMappingConflict")]
    public void should_refuse_drift_other_than_deterministic_container_additions(string drift, string reason)
    {
        Apply(ProposeNote(Targets(SemanticKind.Slice)[0]));
        Refresh();
        var before = Files.Read(McpState.FileName);
        var path = Path.Combine(RootPath, "Orders/PlaceOrder.play");
        var source = File.ReadAllText(path);
        if (drift == "mapping")
        {
            File.Move(path, path + ".moved.play");
        }
        else
        {
            File.WriteAllText(path, drift switch
            {
                "unparsed" => "not valid Screenplay",
                "rename" => source.Replace("PlaceOrder", "Other", StringComparison.Ordinal),
                _ => source.Replace("reads Stock by orderId", string.Empty, StringComparison.Ordinal)
            });
        }

        var failure = Catch.Exception(() => new McpWorkspaces(Root).Open(McpJson.Empty));
        failure.ShouldBeOfExactType<McpFailure>();
        failure.Message.ShouldContain(reason);
        McpManagedFiles.Equal(Files.Read(McpState.FileName), before).ShouldBeTrue();
    }
}
