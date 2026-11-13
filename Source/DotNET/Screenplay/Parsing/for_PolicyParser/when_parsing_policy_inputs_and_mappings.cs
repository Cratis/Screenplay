// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_PolicyParser;

public class when_parsing_policy_inputs_and_mappings : Specification
{
    const string Source = "policy PlanGrants\n  asset AssetId from $context.asset\n  reads AssetPlan as plan by asset\n  require plan.dataQuality == true\n";
    const string Authorizing = "module M\n  feature F\n    slice StateChange S\n      command Request\n        vesselId AssetId identifier\n        authorize PlanGrants\n          asset = vesselId\n";

    ApplicationSyntax _policy;
    ApplicationSyntax _artifact;
    string _printed;
    string _printedArtifact;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _policy = compiler.Parse(Source).Value!;
        _artifact = compiler.Parse(Authorizing).Value!;
        _printed = new ScreenplayPrinter().Print(_policy);
        _printedArtifact = new ScreenplayPrinter().Print(_artifact);
    }

    [Fact] void should_keep_the_input_with_its_type() => _policy.Policies.Single().Inputs!.Single().ShouldMatch(input => input.Name == "asset" && input.Type == "AssetId");
    [Fact] void should_keep_the_default_source() => _policy.Policies.Single().Inputs!.Single().From.ShouldEqual("$context.asset");
    [Fact] void should_print_the_input_with_its_source() => _printed.ShouldContain("asset AssetId from $context.asset");
    [Fact] void should_keep_the_read_keyed_by_the_input() => _policy.Policies.Single().Reads!.Single().By.ShouldEqual("asset");
    [Fact] void should_keep_the_mapping_under_authorize() => _artifact.Modules.Single().Features.Single().Slices.Single().Commands.Single().Authorize!.Mappings!.Single().ShouldMatch(mapping => mapping.Input == "asset" && mapping.Property == "vesselId");
    [Fact] void should_print_the_mapping_under_authorize() => _printedArtifact.ShouldContain("authorize PlanGrants\n          asset = vesselId");
}
