// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.given;

/// <summary>
/// Builds small documents around graph declarations, graph projections and graph specification steps.
/// </summary>
public static class GraphDocuments
{
    public const string Concepts =
        "concept PersonId : Uuid\n" +
        "concept GroupId : Uuid\n" +
        "concept PersonName : String\n" +
        "type Address\n  street String\n";

    public const string Graph =
        "graph Organization\n" +
        "  node Person\n" +
        "    personId PersonId key\n" +
        "    name PersonName\n" +
        "  node Group\n" +
        "    groupId GroupId key\n" +
        "  edge MEMBER_OF source Person target Group\n" +
        "    since DateTime\n";

    public const string Events =
        "event PersonRegistered\n  name PersonName\n" +
        "event PersonRemoved\n" +
        "event MemberAdded\n  groupId GroupId\n";

    public const string Projection =
        "projection OrganizationGraph => graph Organization\n" +
        "  node Person\n" +
        "    from PersonRegistered key $eventSourceId\n" +
        "      name = name\n" +
        "    remove with PersonRemoved key $eventSourceId detach\n" +
        "  edge MEMBER_OF\n" +
        "    from MemberAdded source $eventSourceId target groupId\n" +
        "      since = $eventContext.occurred\n";

    /// <summary>
    /// Wraps slice members in an application with the shared concepts.
    /// </summary>
    /// <param name="members">The slice members, each line indented by the caller relative to the slice.</param>
    /// <returns>The document text.</returns>
    public static string Document(params string[] members) =>
        Concepts + "module M\n  feature F\n    slice StateView S\n" + string.Concat(members.Select(Indent));

    /// <summary>
    /// Indents a block by six spaces, the depth of a slice member.
    /// </summary>
    /// <param name="block">The block to indent.</param>
    /// <returns>The indented block.</returns>
    public static string Indent(string block) =>
        string.Concat(block.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => "      " + line + "\n"));
}
