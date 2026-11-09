// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    /// <summary>The v41 source plus explicitly exposed executable query and command declarations.</summary>
    public const string AgentExposureSource = GraphForEachSource + """

        module AgentSurface
          feature Operations
            slice StateView Directory
              readmodel AgentPerson
                personId PersonId key
              query People => AgentPerson[]
            slice StateChange Actions
              command Refresh
                personId PersonId identifier

        agents
          tokens personal
          query AgentSurface.Operations.Directory.People
            scope read
            description "Read the directory."
          command AgentSurface.Operations.Actions.Refresh
            scope write
            description "Refresh the directory."
        """;

    /// <summary>Gets the canonical v42 bytes with an explicit query and command allowlist.</summary>
    public static byte[] EsmV42Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v42.json");

    /// <summary>Creates the v42 executable model without changing existing authorization.</summary>
    /// <returns>The v42 executable model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV42()
    {
        var result = property_order_binding.BindDefault(AgentExposureSource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value!.Model;
    }
}
#endif
