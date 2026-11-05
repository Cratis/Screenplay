// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v38 golden: a graph with a node, an edge and an identified edge, the projection that builds it, and a specification that seeds, appends and asserts.
    /// </summary>
    public const string GraphSource = """
        command reads protected

        concept PersonId : Uuid
        concept GroupId : Uuid
        concept PersonName : String

        module Organization
          feature Chart
            slice StateView OrganizationChart
              event PersonRegistered
                name PersonName
              event PersonRemoved
              event MemberAdded
                groupId GroupId
              event RoleAssigned
                groupId GroupId
                role    String

              graph Organization
                node Person
                  personId PersonId key
                  name     PersonName
                  visits   Int optional
                node Group
                  groupId GroupId key
                edge MEMBER_OF source Person target Group
                  since DateTime optional
                edge HAS_ROLE source Person target Group identified by role
                  role String

              projection OrganizationChart => graph Organization
                node Person
                  every
                    increment visits
                  from PersonRegistered key $eventSourceId
                    personId = $eventSourceId
                  remove with PersonRemoved key $eventSourceId detach
                node Group
                  from MemberAdded key groupId
                    groupId = groupId
                edge MEMBER_OF
                  from MemberAdded source $eventSourceId target groupId
                    since = $eventContext.occurred
                edge HAS_ROLE
                  from RoleAssigned source $eventSourceId target groupId identified by role

              specification AddingAMemberDrawsAnEdge
                given graph Organization
                  has node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                    name = "Ada"
                when append MemberAdded
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  groupId = "0f8fad5b-d9cb-469f-a165-70867728950e"
                then graph Organization
                  has node Group "0f8fad5b-d9cb-469f-a165-70867728950e"
                  has edge MEMBER_OF from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e"
                  no edge HAS_ROLE from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e" identified by "Lead"
        """;

    public static byte[] EsmV38Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v38.json");

    public static ExecutableSemanticModel CreateSemanticModelV38()
    {
        var result = property_order_binding.BindDefault(GraphSource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value!.Model;
    }
}
#endif
