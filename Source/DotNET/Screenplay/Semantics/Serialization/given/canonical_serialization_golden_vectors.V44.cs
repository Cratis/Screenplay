// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The v43 source plus explicitly exposed executable query and command declarations.
    /// </summary>
    public const string AgentExposureSource = ProjectionParitySource + """

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

    /// <summary>
    /// Gets the canonical v44 bytes with an explicit query and command allowlist.
    /// </summary>
    public static byte[] EsmV44Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v44.json");

    /// <summary>
    /// Creates the v44 executable model without changing existing authorization.
    /// </summary>
    /// <returns>The v44 executable model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV44()
    {
        var result = property_order_binding.AdmitScreens(AgentExposureSource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
        return result.Compilation!.Model;
    }
}
#endif
