// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_refusing_query_call_arguments : given.a_semantic_binder
{
    const string Prefix = "module Items\n  feature Browse\n    slice StateView List\n      readmodel Item\n        itemId String\n        status String\n      query Find => Item[]\n        by itemId String\n        filter status String\n      specification Check\n";

    [Theory]
    [InlineData(true, "unknown")]
    [InlineData(false, "unknown")]
    [InlineData(true, "repeated")]
    [InlineData(false, "repeated")]
    [InlineData(true, "filter")]
    [InlineData(false, "filter")]
    [InlineData(true, "by")]
    [InlineData(false, "by")]
    void should_report_play0468_for_both_argument_spellings(bool performed, string kind)
    {
        var indent = performed ? "          " : "            ";
        var values = kind switch
        {
            "unknown" => new[] { "itemId = \"a\"", "unknown = \"open\"" },
            "repeated" => ["itemId = \"a\"", "status = \"open\"", "status = \"open\""],
            "filter" => ["itemId = \"a\""],
            _ => ["status = \"open\""]
        };
        var source = Prefix + (performed ? "        when query Find\n" : "        then query Find\n          arguments\n") +
            string.Join('\n', values.Select(value => indent + value)) + (performed ? "\n        then no result\n" : "\n");
        var result = Bind(source);
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownSpecificationQueryArgument);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_refuse_null_filter_values(bool performed)
    {
        var source = Prefix + (performed
            ? "        when query Find\n          itemId = \"a\"\n          status = null\n        then no result\n"
            : "        then query Find\n          arguments\n            itemId = \"a\"\n            status = null\n");
        Bind(source).Success.ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_report_repeated_source_arguments_before_binding(bool performed)
    {
        var source = Prefix + (performed
            ? "        when query Find\n          itemId = \"a\"\n          status = \"open\"\n          status = \"open\"\n        then no result\n"
            : "        then query Find\n          arguments\n            itemId = \"a\"\n            status = \"open\"\n            status = \"open\"\n");
        new ScreenplayCompiler().Parse(source).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownSpecificationQueryArgument);
    }
}
