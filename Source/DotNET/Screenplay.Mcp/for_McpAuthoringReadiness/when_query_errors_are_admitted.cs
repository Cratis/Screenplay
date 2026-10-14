// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringReadiness;

public class when_query_errors_are_admitted : Specification
{
    McpAuthoringReadiness _readiness;

    void Establish() => _readiness = new(new ScreenplayCompiler().Parse("concept Number : String\n  validate\n    min 2\nmodule Billing\n  feature Invoices\n    slice StateView Lookup\n      readmodel Invoice\n        number Number\n      query GetInvoice => Invoice optional\n        by number Number\n      specification InvalidNumber\n        when query GetInvoice\n          number = \"A\"\n        then error").Value!);

    [Fact] void should_not_mark_the_model_as_syntax_only() => _readiness.ModelSyntaxOnly.ShouldBeFalse();
}
