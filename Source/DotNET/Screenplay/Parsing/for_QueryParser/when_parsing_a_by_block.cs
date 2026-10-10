// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_QueryParser;

public class when_parsing_a_by_block : Specification
{
    const string Prefix = "module M\n  feature F\n    slice StateView S\n      query Row => View optional\n";
    readonly ScreenplayCompiler _compiler = new();

    [Fact]
    void should_keep_two_parts_in_authored_order()
    {
        var result = _compiler.Parse(Prefix + "        by\n          resourceId String\n          period String from $context.tenant");
        result.Diagnostics.ShouldBeEmpty();
        var query = result.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single();
        query.By.ShouldBeNull();
        query.ByParts.Select(part => part.Name).ShouldEqual("resourceId", "period");
        query.ByParts.Last().Source.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("        by\n          resourceId String")]
    [InlineData("        by\n          resourceId String\n          resourceId String")]
    [InlineData("        by resourceId String\n        by\n          resourceId String\n          period String")]
    [InlineData("        by\n          resourceId String\n          period String\n        by resourceId String")]
    void should_refuse_invalid_shapes(string body) => _compiler.Parse(Prefix + body).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadModelKeyLookup).ShouldBeTrue();
}
