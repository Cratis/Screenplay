// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_covering_parser_keywords : Specification
{
    [Theory]
    [InlineData("require", "policy Access\n  require authenticated\n")]
    [InlineData("and", "policy Access\n  require authenticated and role \"Admin\"\n")]
    [InlineData("or", "policy Access\n  require authenticated or role \"Admin\"\n")]
    [InlineData("not", "policy Access\n  require not authenticated\n")]
    [InlineData("empty", "concept Value : String\n  validate\n    not empty\n")]
    [InlineData("exact", "numbers exact\n")]
    [InlineData("csharp", "policy Access\n  ```csharp\n  return true;\n  ```\n")]
    [InlineData("for", ContractAdmission.Slice + ContractAdmission.Event + ContractAdmission.Command + "      specification Expected\n        given Recorded\n          for \"id\"\n          value = \"test\"\n")]
    void should_include_keywords_accepted_through_non_switch_dispatch(string keyword, string source)
    {
        new ScreenplayCompiler().Parse(source).Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeFalse();
        JsonNode.Parse(ScreenplayContract.Serialize())["keywords"].AsArray().Select(value => value.GetValue<string>()).ShouldContain(keyword);
    }

    [Fact]
    void should_cover_the_compiled_parser_catalog() => JsonNode.Parse(ScreenplayContract.Serialize())["keywords"].AsArray().Select(value => value.GetValue<string>()).ShouldEqual(CompilerContractCatalog.Keywords);
}
