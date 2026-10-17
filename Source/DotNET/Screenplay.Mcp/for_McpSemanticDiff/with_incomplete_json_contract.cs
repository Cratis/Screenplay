// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public partial class when_comparison_is_incomplete
{
    [Fact] void should_preserve_the_incomplete_json_contract() => Diff.GetRawText().ShouldEqual(IncompleteGolden);

    const string IncompleteGolden = """
        {"sourceRevision":"wsrev1:e73f9db69dfab2d83d076369e58ac92c5182b1177fc540fbeea09659658a42ab","beforeRevision":"wsrev1:4e42d028aebde5c6add9fd026c5b3af2da4de88bda5f6c7a6bdf647db9569e86","afterRevision":"wsrev1:e73f9db69dfab2d83d076369e58ac92c5182b1177fc540fbeea09659658a42ab","complete":false,"hasSemanticChange":null,"comparisonLevel":"authoring-structure","executableBeforeAvailable":false,"executableAfterAvailable":false,"sections":[{"section":"declarations","complete":false,"unavailable":["Unassigned authoring declarations use exact kind/address keys only; semantic rename/identity continuity cannot be established."]},{"section":"events","complete":true,"unavailable":[]},{"section":"members","complete":false,"unavailable":["Unassigned authoring declarations use exact kind/address keys only; semantic rename/identity continuity cannot be established."]},{"section":"specifications","complete":true,"unavailable":[]},{"section":"dependants","complete":true,"unavailable":[]},{"section":"identities","complete":false,"unavailable":["Unassigned authoring declarations use exact kind/address keys only; semantic rename/identity continuity cannot be established."]}],"limits":["Structural authoring comparison, not an equivalence or execution verdict.","Opaque inline content and file references are compared by hash, but behavior inside code and external file contents are not analyzed; use implementation-requirements for attachment content hashes.","Direct indexed dependants only (before and after); properties use their owner\u0027s references and containers aggregate external references to contained declarations, excluding references inside the container. No transitive or runtime impact.","Unassigned kinds (including constraints) are compared by exact kind/authoring address only, never claimed as identity-preserving renames.","No revision-to-revision comparison."],"page":{"revision":"wsrev1:e73f9db69dfab2d83d076369e58ac92c5182b1177fc540fbeea09659658a42ab","totalCount":0,"offset":0,"items":[],"nextOffset":null}}
        """;
}
