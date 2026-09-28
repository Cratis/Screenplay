// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

/// <summary>
/// Directives where a later line silently replaces an earlier one (capture source, key and child map,
/// projection parent, table row click, action label and navigation). A comment on a replaced line must
/// keep its own line; it must never join the owner's header comment.
/// </summary>
public class when_printing_comments_on_silently_replaced_directives : given.a_printer
{
    const string Capture = """
        module Sales
          feature Legacy
            slice Translate Sync
              capture Cap // header-capture
                source api // first-source
                  api Old
                  route /old
                source api
                  api New
                  route /new
                key oldId // first-key
                key id
                map
                  status = status
                append Changed
                  when status
                    id = $.id
                children lines identified by n
                  map // first-child-map
                    a = a
                  map
                    b = b
              event Changed
                id Uuid
        """;

    const string ScreenAndProjection = """
        module Billing
          feature Accounts
            slice StateChange Open
              command OpenAccount
                accountId Uuid identifier
                produces AccountOpened
                  for accountId
                  accountId = accountId
              event AccountOpened
                accountId Uuid
              event LineAdded
                accountId Uuid
                n Int
            slice StateView View
              readmodel Account
                accountId Uuid
                lines AccountLine[]
              readmodel AccountLine
                n Int
              projection P => Account // header-proj
                from AccountOpened key accountId
                children lines identified by n
                  from LineAdded key n // header-from
                    parent n // first-parent
                    parent accountId
              query All => Account[]
              screen Accounts // header-screen
                data Account via query All
                table All // header-table
                  column accountId
                  on row-click navigate to Accounts // first-rowclick
                  on row-click navigate to Other
                section actions
                  action OpenAccount // header-action
                    label "Old" // first-label
                    label "New"
                    navigate to Accounts // first-nav
                    navigate to Other
              screen Other
                data Account via query All
        """;

    [Theory]
    [InlineData(Capture)]
    [InlineData(ScreenAndProjection)]
    void should_keep_every_comment_once_on_its_own_line(string source)
    {
        var roundtrip = RoundTrip(source);
        roundtrip.Original!.Success.ShouldBeTrue();
        roundtrip.Reparsed.Success.ShouldBeTrue();
        var authored = source.Split('\n').Select(line => line.IndexOf("// ", StringComparison.Ordinal))
            .Zip(source.Split('\n'), (index, line) => index < 0 ? null : line[(index + 3)..].Trim())
            .Where(marker => marker is not null).ToArray();
        var printed = roundtrip.Printed.Split('\n');
        foreach (var marker in authored)
        {
            printed.Count(line => line.Contains($"// {marker}", StringComparison.Ordinal)).ShouldEqual(1);
        }

        printed.Count(line => line.Split("// ").Length > 2).ShouldEqual(0);
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }
}
