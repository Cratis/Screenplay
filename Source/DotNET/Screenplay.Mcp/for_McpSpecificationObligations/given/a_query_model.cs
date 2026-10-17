// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSpecificationObligations.given;

public class a_query_model : for_McpConnection.given.a_connection
{
    internal const string Model = """
        domain Acme
        concept OwnerId : String
        policy IsOwner
          require claim "memberId" matches ownerId
        module Notes
          feature Pinboard
            slice StateView MyNotes
              readmodel NoteRow
                ownerId OwnerId key
                text String
              query NotesOfOwner => NoteRow optional
                by ownerId OwnerId
                authorize IsOwner
              query OtherNotes => NoteRow optional
                by ownerId OwnerId
                authorize IsOwner
        """;

    void Establish() => Initialize();
}
