// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public const string GraphQuerySource = GraphSource + """

              readmodel Membership
                personId PersonId key
                person   PersonName
                group    GroupId
                since    DateTime optional

              query MembersOf => Membership[] graph Organization
                filter groupId GroupId
                match person:Person -link:MEMBER_OF-> group:Group
                where group.groupId = $groupId
                return
                  personId = person.personId
                  person   = person.name
                  group    = group.groupId
                  since    = link.since

              specification SeeingTheMembers
                given graph Organization
                  has node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                    name = "Ada"
                  has node Group "0f8fad5b-d9cb-469f-a165-70867728950e"
                  has edge MEMBER_OF from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e"
                then query MembersOf
                  arguments
                    groupId = "0f8fad5b-d9cb-469f-a165-70867728950e"
                  result
                    person = "Ada"
        """;

    public static byte[] EsmV40Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v40.json");

    public static ExecutableSemanticModel CreateSemanticModelV40()
    {
        var result = property_order_binding.BindDefault(GraphQuerySource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value!.Model;
    }
}
#endif
