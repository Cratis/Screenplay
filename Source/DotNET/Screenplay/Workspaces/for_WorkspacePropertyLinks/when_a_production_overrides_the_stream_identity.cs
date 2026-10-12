// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_production_overrides_the_stream_identity : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink[] _keys;

    void Establish() => _links = Create("""
        eventsource Accounts
          stream Transactions
            streamId
              owner String
              period Int
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                account String
                month Int
                stream Accounts.Transactions
                  streamId
                    owner = account
                    period = month
                produces Registered
                  stream Accounts.Transactions
                    streamId
                      owner = account
                      period = month
              event Registered
                owner String
                period Int
        """);
    void Because() => _keys = [.. _links.Incoming(_links.Find("Projects.Registration.Register.Register.account").Single())];

    [Fact] void should_index_both_command_and_production_route_sources() => _keys.Length.ShouldEqual(2);
    [Fact] void should_classify_route_sources_as_keys() => _keys.Select(link => link.Reference.Role).Distinct().ShouldContainOnly("key");
    [Fact] void should_not_map_route_parts_to_event_payload_properties() => _links.Links.Where(link => link.Reference.Role == "mapping-target").ShouldBeEmpty();
    [Fact] void should_resolve_each_route_reader() => _keys.Select(link => link.Resolution).Distinct().ShouldContainOnly("resolved");
}
