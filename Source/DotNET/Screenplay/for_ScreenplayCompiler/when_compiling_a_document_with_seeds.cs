// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_seeds : given.a_compiler
{
    const string Source =
        """
        import Customers.CustomerRegistered
        import Customers.CustomerUpgraded

        seed
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            CustomerRegistered
              customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
              name       = "Acme Corp"
            CustomerUpgraded
              tier = "gold"
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
            CustomerRegistered
              name = "Globex"

        seed
          for "1c1e8a5c-9f38-4b62-9c26-3b64a52dd1cf"
            CustomerRegistered
              name = "Initech"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_accumulate_both_seed_blocks() => _result.Value!.Seeds!.Count().ShouldEqual(2);
    [Fact] void should_parse_the_groups_of_the_first_block() => FirstSeed.Groups.Count().ShouldEqual(2);
    [Fact] void should_parse_the_event_source_ids() => FirstSeed.Groups.Select(_ => _.EventSourceId).ShouldContainOnly("3fa85f64-5717-4562-b3fc-2c963f66afa6", "9c858901-8a57-4791-81fe-4c455b099bc9");
    [Fact] void should_parse_the_events_of_the_first_group() => FirstSeed.Groups.First().Events.Select(_ => _.Event).ShouldContainOnly("CustomerRegistered", "CustomerUpgraded");
    [Fact] void should_parse_the_event_properties() => FirstSeed.Groups.First().Events.First().Properties.Count().ShouldEqual(2);
    [Fact] void should_parse_the_property_values() => ((LiteralExpressionSyntax)FirstSeed.Groups.First().Events.Last().Properties.Single().Source).Value.ShouldEqual("gold");
    [Fact] void should_parse_the_second_block() => _result.Value!.Seeds!.Last().Groups.Single().Events.Single().Event.ShouldEqual("CustomerRegistered");

    SeedSyntax FirstSeed => _result.Value!.Seeds!.First();
}
