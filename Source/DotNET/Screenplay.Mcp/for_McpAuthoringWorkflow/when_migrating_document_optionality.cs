// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_migrating_document_optionality : given.a_document_optionality_proposal
{
    [Fact] void should_describe_typed_spelling_operations() => _repair.GetProperty("operations").EnumerateArray().All(operation => operation.GetProperty("operation").GetString() == "migrate-optional-type").ShouldBeTrue();
    [Fact] void should_require_trivia_preservation_by_default() => _repair.GetProperty("requiredFormatting").GetString().ShouldEqual("PreserveTrivia");
    [Fact] void should_not_write_during_discovery_or_preview() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(_before);
    [Fact] void should_keep_every_other_byte() => _preview.ShouldEqual([0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(Types.Replace("String?", "String optional", StringComparison.Ordinal).Replace("String[]?", "String[] optional", StringComparison.Ordinal) + Source)]);
}
