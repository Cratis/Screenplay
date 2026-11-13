// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_a_handler_reads_provided_values : given.a_semantic_binder
{
    const string Source = """
        provide region String from "Nordics"

        module Billing
          provide region String from "Overridden"
          feature Accounts
            slice StateChange Commands
              provide limit Int from 10
              command Deposit
                id Uuid identifier
                handler
                  file Handler.cs
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_describe_the_provided_values_as_context_members() => _result.TypedContextDescriptors.Single().Members.Select(member => member.Name).ShouldContain("region", "limit");
    [Fact] void should_make_every_provided_member_nullable() => _result.TypedContextDescriptors.Single().Members.Where(member => member.Source.Kind == SemanticContextSourceKinds.Provided).All(member => member.IsNullable).ShouldBeTrue();
    [Fact] void should_name_the_provided_value_in_the_source() => _result.TypedContextDescriptors.Single().Members.Single(member => member.Name == "limit").Source.Path.ShouldEqual("limit");
    [Fact] void should_write_revision_four_of_the_sidecar() => System.Text.Encoding.UTF8.GetString(SemanticTypedContextSerializer.Serialize(_result.TypedContextDescriptors)).ShouldContain("\"contractRevision\":4");
    [Fact] void should_name_the_provided_source_kind_in_the_vocabulary() => System.Text.Encoding.UTF8.GetString(SemanticTypedContextSerializer.Serialize(_result.TypedContextDescriptors)).ShouldContain("\"provided\"");
}
