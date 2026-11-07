// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_Users;

public class when_declaring_users : Specification
{
    const string Source =
        """
        policy Staff
          require authenticated

        persona Clerk
          policy Staff

        persona Visitor

        user kari "Kari \"K\" Nordmann" as Clerk, Visitor
        user ola as Clerk
        user guest
        """;

    CompilationResult<ApplicationSyntax> _result;
    UserSyntax[] _users;

    void Establish() => _result = new ScreenplayCompiler().Compile(Source);

    void Because() => _users = [.. _result.Value!.Users];

    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_every_user_in_order() => _users.Select(user => user.Name).ShouldEqual(["kari", "ola", "guest"]);
    [Fact] void should_unescape_the_display_name() => _users[0].DisplayName.ShouldEqual("Kari \"K\" Nordmann");
    [Fact] void should_keep_the_personas_a_user_plays_in_order() => _users[0].Personas.ShouldEqual(["Clerk", "Visitor"]);
    [Fact] void should_leave_a_missing_display_name_null() => _users[1].DisplayName.ShouldBeNull();
    [Fact] void should_let_a_user_play_no_persona() => _users[2].Personas.ShouldBeEmpty();
    [Fact] void should_print_the_users_the_way_they_were_written() => new ScreenplayPrinter().Print(_result.Value!).ShouldContain("user kari \"Kari \\\"K\\\" Nordmann\" as Clerk, Visitor");
}
