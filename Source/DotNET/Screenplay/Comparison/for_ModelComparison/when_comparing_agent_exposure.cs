// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_comparing_agent_exposure : Cratis.Screenplay.for_Agents.given.an_agent_model
{
    const string Empty = "agents\n  tokens personal\n";
    const string Query = "agents\n  tokens personal\n  query All\n    scope read\n    description \"Find invoices\"\n";
    const string Limit = "Agent exposure was not compared; use the agent-exposure view.";

    [Fact]
    void should_leave_absent_blocks_default_denied_without_a_legacy_limit()
    {
        var result = Compare(Model, Model);
        result.AgentExposure.Rows.ShouldBeEmpty();
        result.AgentExposure.Coverage.ShouldEqual(AgentExposureCoverage.Complete);
        result.AgentExposure.HasWidening.ShouldEqual(false);
        result.NotCompared.Contains(Limit).ShouldBeFalse();
    }

    [Fact]
    void should_treat_an_empty_block_as_an_empty_allowlist()
    {
        var result = Compare(Model, Model + "\n" + Empty);
        result.AgentExposure.Rows.ShouldBeEmpty();
        result.AgentExposure.HasWidening.ShouldEqual(false);
        result.NotCompared.Count(limit => limit == Limit).ShouldEqual(1);
    }

    [Fact]
    void should_warn_when_an_operation_becomes_exposed()
    {
        var result = Compare(Model, Model + "\n" + Query);
        var row = result.AgentExposure.Rows.Single();
        row.Change.ShouldEqual(AgentExposureChangeKind.Added);
        row.DiagnosticCode.ShouldEqual(DiagnosticCodes.NewlyExposedAgentOperation);
        row.QualifiedName.ShouldEqual("Sales.Invoices.Listing.All");
        result.AgentExposure.HasWidening.ShouldEqual(true);
    }

    [Theory]
    [InlineData("")]
    [InlineData(Empty)]
    void should_treat_removed_exposure_as_narrowing_the_allowlist(string after)
    {
        var result = Compare(Model + "\n" + Query, Model + "\n" + after).AgentExposure;
        result.Rows.Single().Change.ShouldEqual(AgentExposureChangeKind.Removed);
        result.HasWidening.ShouldEqual(false);
    }

    [Theory]
    [InlineData("read", "write", AgentExposureChangeKind.Widened)]
    [InlineData("write", "read", AgentExposureChangeKind.Narrowed)]
    [InlineData("read", "read", AgentExposureChangeKind.Unchanged)]
    void should_order_query_scopes(string before, string after, AgentExposureChangeKind expected)
    {
        var result = Compare(Model + "\n" + Query.Replace("scope read", $"scope {before}", StringComparison.Ordinal), Model + "\n" + Query.Replace("scope read", $"scope {after}", StringComparison.Ordinal)).AgentExposure;
        var row = result.Rows.Single();
        row.Change.ShouldEqual(expected);
        row.DiagnosticCode.ShouldEqual(expected == AgentExposureChangeKind.Widened ? DiagnosticCodes.WidenedAgentOperation : null);
    }

    [Fact]
    void should_ignore_exposure_description_edits_and_bare_reference_spelling()
    {
        var after = Query.Replace("query All", "query Sales.Invoices.Listing.All", StringComparison.Ordinal).Replace("Find invoices", "Browse the directory", StringComparison.Ordinal);
        Compare(Model + "\n" + Query, Model + "\n" + after).AgentExposure.Rows.Single().Change.ShouldEqual(AgentExposureChangeKind.Unchanged);
    }

    [Theory]
    [InlineData("Int")]
    [InlineData("Integer")]
    void should_widen_when_an_exposed_query_result_contract_changes(string type)
    {
        var after = Model.Replace("id String", $"id {type}", StringComparison.Ordinal) + "\n" + Query;
        new ScreenplayCompiler().Compile(after).Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeFalse();
        var result = Compare(Model + "\n" + Query, after).AgentExposure;
        result.Coverage.ShouldEqual(AgentExposureCoverage.Complete);
        result.Rows.Single().Change.ShouldEqual(AgentExposureChangeKind.Widened);
        result.Rows.Single().Reason.ShouldContain("exposed contract changed");
        result.HasWidening.ShouldEqual(true);
    }

    [Theory]
    [InlineData("Int")]
    [InlineData("Integer")]
    void should_widen_a_changed_command_even_when_its_scope_is_identical(string type)
    {
        var after = Model.Replace("id String identifier", $"id {type} identifier", StringComparison.Ordinal);
        new ScreenplayCompiler().Compile(after + "\n" + Allowlist).Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeFalse();
        var result = Compare(Model + "\n" + Allowlist, after + "\n" + Allowlist).AgentExposure;
        result.Coverage.ShouldEqual(AgentExposureCoverage.Complete);
        result.HasWidening.ShouldEqual(true);
        result.Rows.Single(row => row.Kind == AgentOperationKind.Command).Reason.ShouldContain("exposed contract changed");
        result.Rows.Single(row => row.Kind == AgentOperationKind.Command).Change.ShouldEqual(AgentExposureChangeKind.Widened);
        result.Rows.Single(row => row.Kind == AgentOperationKind.Query).Change.ShouldEqual(AgentExposureChangeKind.Unchanged);
    }

    [Fact]
    void should_not_widen_for_an_unexposed_operation_change()
    {
        var after = Model.Replace("id String identifier", "id Integer identifier", StringComparison.Ordinal);
        new ScreenplayCompiler().Compile(after + "\n" + Query).Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeFalse();
        Compare(Model + "\n" + Query, after + "\n" + Query).AgentExposure.Rows.Single().Change.ShouldEqual(AgentExposureChangeKind.Unchanged);
    }

    [Theory]
    [InlineData("tokens personal", "tokens oauth")]
    [InlineData("query All", "query Missing")]
    [InlineData("scope read", "scope invalid")]
    void should_fail_closed_for_unresolved_targets_unsupported_tokens_and_parse_errors(string original, string replacement)
    {
        var invalid = Model + "\n" + Query.Replace(original, replacement, StringComparison.Ordinal);
        new ScreenplayCompiler().Compile(invalid).Success.ShouldBeFalse();
        var result = Compare(Model + "\n" + Query, invalid);
        result.AgentExposure.Coverage.ShouldEqual(AgentExposureCoverage.Incomplete);
        result.AgentExposure.HasWidening.ShouldBeNull();
        result.AgentExposure.Rows.All(row => row.Change == AgentExposureChangeKind.Unknown).ShouldBeTrue();
        result.NotCompared.Count(limit => limit == Limit).ShouldEqual(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_make_every_row_unknown_when_either_source_has_errors(bool baselineInvalid)
    {
        const string valid = Model + "\n" + Allowlist;
        var invalid = valid.Replace("scope read", "scope invalid", StringComparison.Ordinal);
        var result = baselineInvalid ? Compare(invalid, valid) : Compare(valid, invalid);
        result.AgentExposure.Coverage.ShouldEqual(AgentExposureCoverage.Incomplete);
        result.AgentExposure.Rows.All(row => row.Change == AgentExposureChangeKind.Unknown).ShouldBeTrue();
        result.AgentExposure.HasWidening.ShouldBeNull();
    }

    [Fact]
    void should_disclose_errors_even_when_neither_block_survives()
    {
        var invalid = Model.Replace("query All => Invoice[]", "query", StringComparison.Ordinal);
        new ScreenplayCompiler().Compile(invalid).Success.ShouldBeFalse();
        var result = Compare(invalid, Model);
        result.AgentExposure.Coverage.ShouldEqual(AgentExposureCoverage.Incomplete);
        result.AgentExposure.HasWidening.ShouldBeNull();
        result.NotCompared.Count(limit => limit == Limit).ShouldEqual(1);
    }

    [Fact]
    void should_separate_preview_binding_refusal_from_source_errors()
    {
        var preview = "system Store\n" + Model.Replace("      readmodel Invoice", "      operation LoadInvoices\n        uses Store\n        invoiceId Uuid\n      readmodel Invoice", StringComparison.Ordinal);
        new ScreenplayCompiler().Compile(preview).Diagnostics.ShouldBeEmpty();
        Source(preview).Workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
        var result = Compare(preview, preview);
        result.AgentExposure.Coverage.ShouldEqual(AgentExposureCoverage.Complete);
        result.AgentExposure.Rows.ShouldBeEmpty();
        result.NotCompared.Contains(Limit).ShouldBeFalse();
    }

    [Fact]
    void should_compare_intact_allowlists_even_when_preview_binding_refuses()
    {
        var preview = "system Store\n" + Model.Replace("      readmodel Invoice", "      operation LoadInvoices\n        uses Store\n        invoiceId Uuid\n      readmodel Invoice", StringComparison.Ordinal);
        Source(preview + "\n" + Query).Workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
        var result = Compare(preview, preview + "\n" + Query).AgentExposure;
        result.Coverage.ShouldEqual(AgentExposureCoverage.Complete);
        result.Rows.Single().Change.ShouldEqual(AgentExposureChangeKind.Added);
    }

    [Fact]
    void should_report_a_renamed_exposed_operation_as_removed_and_added()
    {
        var after = (Model + "\n" + Allowlist).Replace("Issue", "IssueInvoice", StringComparison.Ordinal);
        var result = Compare(Model + "\n" + Allowlist, after).AgentExposure;
        result.Coverage.ShouldEqual(AgentExposureCoverage.Complete);
        result.Rows.Single(row => row.QualifiedName.EndsWith(".Issue", StringComparison.Ordinal)).Change.ShouldEqual(AgentExposureChangeKind.Removed);
        result.Rows.Single(row => row.QualifiedName.EndsWith(".IssueInvoice", StringComparison.Ordinal)).Change.ShouldEqual(AgentExposureChangeKind.Added);
        result.HasWidening.ShouldEqual(true);
    }

    [Fact]
    void should_not_equate_a_query_with_a_command_at_the_same_address()
    {
        var before = Model + "\n" + Query.Replace("scope read", "scope write", StringComparison.Ordinal);
        var after = before.Replace("query All => Invoice[]", "command All\n        id String identifier", StringComparison.Ordinal)
            .Replace("query All\n", "command All\n", StringComparison.Ordinal);
        new ScreenplayCompiler().Compile(after).Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeFalse();
        var result = Compare(before, after).AgentExposure;
        result.Coverage.ShouldEqual(AgentExposureCoverage.Complete);
        result.Rows.Single(row => row.Kind == AgentOperationKind.Query).Change.ShouldEqual(AgentExposureChangeKind.Removed);
        result.Rows.Single(row => row.Kind == AgentOperationKind.Command).Change.ShouldEqual(AgentExposureChangeKind.Added);
    }

    [Fact]
    void should_order_rows_independently_of_authored_allowlist_order()
    {
        const string reversed = "agents\n  tokens personal\n  command Sales.Invoices.Issuing.Issue\n    scope write\n    description \"Issue\"\n  query All\n    scope read\n    description \"Read\"\n";
        var forward = Compare(Model, Model + "\n" + Allowlist).AgentExposure;
        var backward = Compare(Model, Model + "\n" + reversed).AgentExposure;
        forward.Rows.Select(row => (row.Kind, row.QualifiedName)).SequenceEqual(backward.Rows.Select(row => (row.Kind, row.QualifiedName))).ShouldBeTrue();
    }

    [Theory]
    [InlineData("concept Identifier : String", "concept Identifier : Int")]
    [InlineData("value Identifier", "value String")]
    [InlineData("details Details", "details Details[]")]
    void should_follow_transitive_query_result_contracts(string original, string replacement)
    {
        var before = "concept Identifier : String\ntype Details\n  value Identifier\n" + Model.Replace("id String\n", "details Details\n", StringComparison.Ordinal) + "\n" + Query;
        var after = before.Replace(original, replacement, StringComparison.Ordinal);
        new ScreenplayCompiler().Compile(before).Success.ShouldBeTrue();
        new ScreenplayCompiler().Compile(after).Success.ShouldBeTrue();
        var exposure = Compare(before, after).AgentExposure;
        exposure.Coverage.ShouldEqual(AgentExposureCoverage.Complete);
        exposure.Rows.Single().Change.ShouldEqual(AgentExposureChangeKind.Widened);
        exposure.HasWidening.ShouldEqual(true);
    }

    [Fact]
    void should_follow_transitive_command_property_contracts()
    {
        var before = "concept Identifier : String\n" + Model.Replace("id String identifier", "id Identifier identifier", StringComparison.Ordinal) + "\n" + Allowlist;
        var after = before.Replace("concept Identifier : String", "concept Identifier : Int", StringComparison.Ordinal);
        new ScreenplayCompiler().Compile(after).Success.ShouldBeTrue();
        var exposure = Compare(before, after).AgentExposure;
        exposure.Rows.Single(row => row.Kind == AgentOperationKind.Command).Change.ShouldEqual(AgentExposureChangeKind.Widened);
        exposure.Rows.Single(row => row.Kind == AgentOperationKind.Query).Change.ShouldEqual(AgentExposureChangeKind.Unchanged);
    }

    [Fact]
    void should_follow_inherited_authorization_without_hashing_unrelated_module_members()
    {
        const string before = Model + "\n" + Query;
        var after = before.Replace("require role \"Staff\"", "require role \"Managers\"", StringComparison.Ordinal);
        Compare(before, after).AgentExposure.Rows.Single().Change.ShouldEqual(AgentExposureChangeKind.Widened);
        var unrelated = before.Replace("id String identifier", "id Int identifier", StringComparison.Ordinal);
        Compare(before, unrelated).AgentExposure.Rows.Single().Change.ShouldEqual(AgentExposureChangeKind.Unchanged);
    }

    [Fact]
    void should_not_hash_locations_or_unused_type_contracts()
    {
        const string before = "concept Unused : String\n" + Model + "\n" + Query;
        var after = before.Replace("concept Unused : String", "// moved\nconcept Unused : Int", StringComparison.Ordinal);
        Compare(before, after).AgentExposure.Rows.Single().Change.ShouldEqual(AgentExposureChangeKind.Unchanged);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    void should_recover_every_operation_from_duplicate_blocks(bool baselineInvalid, bool sameFile)
    {
        const string hidden = "agents\n  tokens personal\n  command Sales.Invoices.Issuing.Issue\n    scope write\n    description \"Issue\"\n";
        var invalid = sameFile
            ? Source(Model + "\n" + Query + "\n" + hidden)
            : ComparedModel.FromSources("Sales", new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["model.play"] = Model,
                ["first.play"] = Query,
                ["second.play"] = hidden
            });
        var valid = Source(Model + "\n" + Query);
        var result = baselineInvalid ? ModelComparison.Compare(invalid, valid) : ModelComparison.Compare(valid, invalid);
        result.AgentExposure.Coverage.ShouldEqual(AgentExposureCoverage.Incomplete);
        result.AgentExposure.HasWidening.ShouldBeNull();
        result.AgentExposure.Rows.Length.ShouldEqual(2);
        result.AgentExposure.Rows.All(row => row.Change == AgentExposureChangeKind.Unknown).ShouldBeTrue();
        result.AgentExposure.Rows.Any(row => row.QualifiedName.EndsWith(".Issue", StringComparison.Ordinal)).ShouldBeTrue();
        result.NotCompared.Count(limit => limit == Limit).ShouldEqual(1);
    }

    [Fact]
    void should_not_recover_agent_lookalikes_from_comments_or_code_fences()
    {
        var before = Model.Replace("id String identifier", "id String identifier\n        handler\n          ```csharp\n          // agents\n          //   query Hidden\n          return;\n          ```", StringComparison.Ordinal) + "\n" + Query;
        var after = before + "\n// agents\n//   query Hidden\n";
        var exposure = Compare(before, after).AgentExposure;
        exposure.Rows.Length.ShouldEqual(1);
        exposure.Rows.Single().Change.ShouldEqual(AgentExposureChangeKind.Unchanged);
    }

    static ModelDifference Compare(string before, string after) => ModelComparison.Compare(Source(before), Source(after));

    static ComparedModel Source(string source) => ComparedModel.FromSources("Sales", new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["model.play"] = source
    });
}
