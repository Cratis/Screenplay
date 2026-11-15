// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner;

public class when_selecting_the_workflow_subject : Specification
{
    const string Subject = "9c858901-8a57-4791-81fe-4c455b099bc9";
    const string Other = "c89cdb3c-2035-43e8-96e7-98d996842dc0";
    ImmutableArray<SemanticLifecycleSpecificationResult> _results;

    void Because()
    {
        var declarations = a_lifecycle_source.Source[..a_lifecycle_source.Source.IndexOf("statemachine InvoiceLifecycle", StringComparison.Ordinal)];
        const string deciding = """
            decide Check when overdue == true
              "yes" -> Tag
              "no" -> Done
            action Tag invokes TagInvoice
              next -> Done
            join Done any
            """;
        const string iterating = """
            foreach Check over lines
              each -> Tag
              completed -> Done
            action Tag invokes TagInvoice
            join Done any
            """;
        var source = declarations + Workflow("Deciding", deciding, false) + Workflow("Iterating", iterating, true);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        const string documentKey = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(documentKey), documentKey, "application.play", source);
        var parsed = new ScreenplayCompiler().Parse(source);
        parsed.Value.ShouldNotBeNull();
        var bound = new SemanticModelBinder(2026).Bind("Billing", parsed.Value, SemanticDocumentSet.Create([document], catalog));
        bound.Value.ShouldNotBeNull();
        _results = SemanticLifecycleRunner.RunAll(bound.Value.Model);
    }

    [Fact] void should_not_decide_using_the_sole_other_instance() => Failures("Deciding", "Absent").ShouldEqual(string.Empty);
    [Fact] void should_decide_using_the_matching_instance_alongside_another() => Failures("Deciding", "Matching").ShouldEqual(string.Empty);
    [Fact] void should_decide_without_a_subject_when_no_instances_exist() => Failures("Deciding", "Empty").ShouldEqual(string.Empty);
    [Fact] void should_not_iterate_the_sole_other_instance() => Failures("Iterating", "Absent").ShouldEqual(string.Empty);
    [Fact] void should_iterate_the_matching_instance_alongside_another() => Failures("Iterating", "Matching").ShouldEqual(string.Empty);
    [Fact] void should_not_iterate_when_no_instances_exist() => Failures("Iterating", "Empty").ShouldEqual(string.Empty);

    string Failures(string owner, string name) => string.Join("; ", _results.Single(result => result.Owner == owner && result.Name == name).Failures);

    static string Workflow(string name, string nodes, bool iterating) =>
        $"workflow {name}\n  for Invoice\n  trigger Started manual\n    -> Check\n" +
        string.Join('\n', nodes.Split('\n').Select(line => $"  {line}")) + "\n\n" +
        Scenario("Absent", Registered(Other, false) + Overdue(Other), 0) +
        Scenario("Matching", Registered(Other, false) + Registered(Subject, true) + Overdue(Subject), iterating ? 2 : 1) +
        Scenario("Empty", string.Empty, 0);

    static string Scenario(string name, string given, int invocations) =>
        $"  specification {name}\n" + given + $"    when start \"{Subject}\"\n" +
        string.Concat(Enumerable.Repeat("    then invokes TagInvoice\n", invocations)) + "    then completes\n\n";

    static string Registered(string identity, bool twoLines) =>
        $"    given InvoiceRegistered\n      for \"{identity}\"\n      lines\n        item\n          sku = \"A\"\n" +
        (twoLines ? "        item\n          sku = \"B\"\n" : string.Empty);

    static string Overdue(string identity) => $"    given InvoiceMarkedOverdue\n      for \"{identity}\"\n";
}
