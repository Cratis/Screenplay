// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticGraphRunner;

public class when_running_graph_specifications : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Person = "6fa459ea-ee8a-4ca4-894e-db77e160355e";
    const string Other = "9c858901-8a57-4791-81fe-4c455b099bc9";
    const string Group = "0f8fad5b-d9cb-469f-a165-70867728950e";

    const string Model =
        "command reads protected\n" +
        "concept PersonId : Uuid\nconcept GroupId : Uuid\n" +
        "module M\n  feature F\n    slice StateView S\n" +
        "      event PersonRegistered\n        name String\n" +
        "      event PersonRemoved\n" +
        "      event Visited\n        groupId GroupId\n        points Int\n" +
        "      event Left\n        groupId GroupId\n" +
        "      event Joined\n        groupId GroupId\n        role String\n" +
        "      event Untagged\n        groupId GroupId\n        role String\n" +
        "      graph G\n        node Person\n          personId PersonId key\n          name String\n          visits Int optional\n" +
        "        node Group\n          groupId GroupId key\n" +
        "        edge VISITED source Person target Group\n          points Int optional\n" +
        "        edge TAGGED source Person target Group identified by role\n          role String\n" +
        "      projection P => graph G\n" +
        "        node Person\n" +
        "          from PersonRegistered key $eventSourceId\n            personId = $eventSourceId\n" +
        "          remove with PersonRemoved key $eventSourceId detach\n" +
        "          from Visited key $eventSourceId\n            increment visits\n" +
        "        edge VISITED\n" +
        "          from Visited source $eventSourceId target groupId\n            add points by points\n" +
        "          remove with Left source $eventSourceId target groupId\n" +
        "        edge TAGGED\n" +
        "          from Joined source $eventSourceId target groupId identified by role\n" +
        "          remove with Untagged source $eventSourceId target groupId identified by role\n";

    static string Specification(string steps) => $"{Model}      specification S\n{steps}";

    IReadOnlyList<SemanticGraphSpecificationResult> Run(string steps)
    {
        var result = Bind(Specification(steps));
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return SemanticGraphRunner.RunAll(result.Value!.Model);
    }

    static string Registered(string key = Person) => $"        given PersonRegistered\n          for \"{key}\"\n          name = \"Ada\"\n";

    [Fact]
    void should_create_a_node_and_set_its_properties() =>
        Run($"{Registered()}        when append Visited\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          points = 2\n        then graph G\n          has node Person \"{Person}\"\n            name = \"Ada\"\n            visits = 1\n").Single().Failures.ShouldBeEmpty();

    [Fact]
    void should_ensure_the_endpoints_of_an_edge() =>
        Run($"        when append Visited\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          points = 2\n        then graph G\n          has node Group \"{Group}\"\n          has node Person \"{Person}\"\n          has edge VISITED from \"{Person}\" to \"{Group}\"\n            points = 2\n").Single().Failures.ShouldBeEmpty();

    [Fact]
    void should_apply_a_counter_to_an_edge_property() =>
        Run($"        given Visited\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          points = 2\n        when append Visited\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          points = 3\n        then graph G\n          has edge VISITED from \"{Person}\" to \"{Group}\"\n            points = 5\n").Single().Failures.ShouldBeEmpty();

    [Fact]
    void should_delete_an_edge() =>
        Run($"        given Visited\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          points = 2\n        when append Left\n          for \"{Person}\"\n          groupId = \"{Group}\"\n        then graph G\n          no edge VISITED from \"{Person}\" to \"{Group}\"\n          has node Group \"{Group}\"\n").Single().Failures.ShouldBeEmpty();

    [Fact]
    void should_delete_a_node_and_detach_its_edges() =>
        Run($"{Registered()}        given Visited\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          points = 2\n        when append PersonRemoved\n          for \"{Person}\"\n        then graph G\n          no node Person \"{Person}\"\n          no edge VISITED from \"{Person}\" to \"{Group}\"\n          has node Group \"{Group}\"\n").Single().Failures.ShouldBeEmpty();

    [Fact]
    void should_keep_edges_identified_by_a_property_apart() =>
        Run($"        given Joined\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          role = \"a\"\n        when append Joined\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          role = \"b\"\n        then graph G\n          has edge TAGGED from \"{Person}\" to \"{Group}\" identified by \"a\"\n          has edge TAGGED from \"{Person}\" to \"{Group}\" identified by \"b\"\n").Single().Failures.ShouldBeEmpty();

    [Fact]
    void should_delete_only_the_identified_edge() =>
        Run($"        given Joined\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          role = \"a\"\n        given Joined\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          role = \"b\"\n        when append Untagged\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          role = \"a\"\n        then graph G\n          no edge TAGGED from \"{Person}\" to \"{Group}\" identified by \"a\"\n          has edge TAGGED from \"{Person}\" to \"{Group}\" identified by \"b\"\n").Single().Failures.ShouldBeEmpty();

    [Fact]
    void should_seed_a_graph_before_the_events() =>
        Run($"        given graph G\n          has node Person \"{Other}\"\n            name = \"Grace\"\n        when append Visited\n          for \"{Other}\"\n          groupId = \"{Group}\"\n          points = 1\n        then graph G\n          has node Person \"{Other}\"\n            name = \"Grace\"\n            visits = 1\n").Single().Failures.ShouldBeEmpty();

    [Fact]
    void should_fail_a_missing_node() =>
        Run($"        when append Visited\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          points = 1\n        then graph G\n          has node Person \"{Other}\"\n").Single().Failures.Single().ShouldContain("does not exist");

    [Fact]
    void should_fail_a_wrong_property_value() =>
        Run($"{Registered()}        when append Visited\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          points = 1\n        then graph G\n          has node Person \"{Person}\"\n            name = \"Grace\"\n").Single().Failures.Single().ShouldContain("holds Ada");

    [Fact]
    void should_fail_an_absence_that_does_not_hold() =>
        Run($"{Registered()}        when append Visited\n          for \"{Person}\"\n          groupId = \"{Group}\"\n          points = 1\n        then graph G\n          no node Person \"{Person}\"\n          no edge VISITED from \"{Person}\" to \"{Group}\"\n").Single().Failures.Length.ShouldEqual(2);
}
