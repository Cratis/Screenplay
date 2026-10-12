// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.given;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_query_filters : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(query_filter_source.Source);

    [Fact] void should_select_v14() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V14);
    [Fact] void should_bind_both_argument_spellings() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_optional_omission() => Queries.Single(query => query.Name == "Filtered").Filters.Last().Type.IsOptional.ShouldBeTrue();
    [Fact] void should_map_filter_source_locations() => Queries.SelectMany(query => query.Filters).All(filter => _result.Value!.SourceMap.Entries.Any(entry => entry.SemanticId == filter.Id)).ShouldBeTrue();

    [Fact]
    void should_register_stable_workspace_filter_addresses()
    {
        var syntax = new ScreenplayCompiler().Parse(query_filter_source.Source).Value!;
        var entries = WorkspaceSyntaxIndex.ForSyntax(syntax, _result.Value!.Documents.IdentityCatalog);
        var slice = SemanticAddress.ForSlice(_applicationIdentity, "Items", "Browsing", "Browse");
        foreach (var query in Queries)
        {
            foreach (var filter in query.Filters)
            {
                var address = SemanticAddress.ForQueryArgument(SemanticAddress.ForQuery(slice, query.Name), filter.Name);
                _result.Value.Documents.IdentityCatalog.ResolveSemantic(address).ShouldEqual(filter.Id);
                entries.ShouldContain(entry => address.Equals(entry.Address));
            }
        }
    }

    [Theory]
    [InlineData("filter status String from $context.identity.id", "uses 'from'")]
    [InlineData("filter unknown String", "same-named")]
    [InlineData("filter status Uuid", "same type")]
    [InlineData("filter status String[]", "scalar")]
    [InlineData("filter status String\n        filter status String", "repeated")]
    void should_refuse_unadmitted_filter_shapes(string replacement, string reason)
    {
        var result = Bind(query_filter_source.Source.Replace("filter status String", replacement, StringComparison.Ordinal));
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains(reason, StringComparison.Ordinal) && diagnostic.Message.Contains("filter '", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("status = \"open\"", "unknown = \"open\"")]
    [InlineData("status = \"open\"", "category = \"x\"")]
    [InlineData("status = \"open\"", "status = \"open\"\n          status = \"open\"")]
    void should_refuse_unknown_missing_or_repeated_specification_arguments(string oldValue, string newValue)
    {
        var result = Bind(query_filter_source.Source.Replace(oldValue, newValue, StringComparison.Ordinal));
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownSpecificationQueryArgument);
    }

    IEnumerable<SemanticKeyedQuery> Queries => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Queries;
}
