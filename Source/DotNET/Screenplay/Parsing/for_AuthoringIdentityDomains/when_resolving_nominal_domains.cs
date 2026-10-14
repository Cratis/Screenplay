// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_AuthoringIdentityDomains;

public class when_resolving_nominal_domains : Specification
{
    AuthoringIdentityDomains _domains;
    readonly DeclarationScope _scope = new(["M", "F", "S"]);

    void Establish()
    {
        var application = new ScreenplayCompiler().Parse("concept ResourceId : String\nmodule M").Value! with
        {
            Concepts = [new("One.ResourceId", "String", [], [], SourceLocation.Start), new("Two.ResourceId", "String", [], [], new(2, 1))],
            Types = [new("Three.ResourceId", [], new(3, 1))]
        };
        _domains = new(application, new(application, []));
    }

    [Fact] void should_distinguish_qualified_concepts_with_the_same_short_name() => Domain("One.ResourceId").ShouldNotEqual(Domain("Two.ResourceId"));
    [Fact] void should_keep_the_complete_declared_identity() => Domain("One.ResourceId")!.Target.ShouldEqual("One.ResourceId");
    [Fact] void should_not_use_a_short_ambiguous_name() => Domain("ResourceId").ShouldBeNull();
    [Fact] void should_not_equate_a_primitive_with_a_concept() => Domain("String").ShouldNotEqual(Domain("One.ResourceId"));
    [Fact] void should_identify_a_composite_by_its_resolved_declaration() => Domain("Three.ResourceId")!.Kind.ShouldEqual("composite");
    [Fact] void should_not_guess_an_unresolved_name() => Domain("Missing.ResourceId").ShouldBeNull();

    AuthoringIdentityDomain? Domain(string name) => _domains.Resolve(new(name, false, false, SourceLocation.Start), _scope);
}
