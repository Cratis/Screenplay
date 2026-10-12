// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_removing_an_example_assertion_property : given.an_authoring_connection
{
    const string Source = """
        module Projects
          feature Registration
            slice StateChange Facts
              event Audited
                name String
                other String
              example Expected : Audited
                name = "Ada"
                other = "kept"
              specification Audit
                when append Audited
                  name = "Ada"
                  other = "kept"
                then Expected
        """;

    JsonElement _proposal;
    JsonElement _review;
    JsonElement _applied;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source, new UTF8Encoding(false));
        Initialize();
    }

    void Because()
    {
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var target = Result("read-ast", new { expectedRevision = revision, kind = "PropertySyntax", name = "name" }).GetProperty("page").GetProperty("items").EnumerateArray().Single().GetProperty("handle");
        _proposal = Result("propose-property", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            action = "remove", target, expectedName = "name", eventNeverPersisted = true,
            formatting = "CanonicalizeTouchedDocuments"
        });
        _review = Result("read-proposal", new { proposalId = _proposal.GetProperty("proposalId").GetString(), view = "semantic-diff" });
        _applied = Apply(opened, _proposal);
    }

    [Fact] void should_match_the_assertion_provenance_result_json() => given.property_result_snapshots.Verify("remove-example-assertion", _proposal.GetProperty("property"));
    [Fact] void should_keep_disclosure_in_the_retained_proposal() => _review.GetProperty("property").GetRawText().ShouldEqual(_proposal.GetProperty("property").GetRawText());
    [Fact] void should_disclose_the_inherited_assertion_once() => _proposal.GetProperty("property").GetProperty("changed").EnumerateArray().Count(change => change.GetProperty("weakensAssertion").GetBoolean()).ShouldEqual(1);
    [Fact] void should_apply_the_disclosed_plan() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
}
