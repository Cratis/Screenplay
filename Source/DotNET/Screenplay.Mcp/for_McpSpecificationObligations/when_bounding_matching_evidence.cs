// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationObligations;

public class when_bounding_matching_evidence : given.a_model
{
    JsonElement _success;

    void Establish()
    {
        var cases = string.Join('\n', Enumerable.Range(0, 21).Select(number => $"      specification Happy{number}\n        when RegisterProject\n          name = \"First\"\n        then ProjectRegistered\n          name = \"First\""));
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Model.Replace("    slice StateView List", cases + "\n    slice StateView List", StringComparison.Ordinal));
    }

    void Because() => _success = Call("find-specification-obligations").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray().Single(item => item.GetProperty("ruleId").GetString() == "SPEC001");

    [Fact] void should_bound_matching_specifications() => _success.GetProperty("specifications").GetArrayLength().ShouldEqual(20);
    [Fact] void should_keep_the_complete_count() => _success.GetProperty("specificationCount").GetInt32().ShouldEqual(22);
    [Fact] void should_disclose_omitted_evidence() => _success.GetProperty("specificationsTruncated").GetBoolean().ShouldBeTrue();
}
