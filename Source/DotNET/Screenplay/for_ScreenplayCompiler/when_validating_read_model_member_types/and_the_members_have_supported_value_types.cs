// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_read_model_member_types;

public class and_the_members_have_supported_value_types : given.a_member
{
    protected override string SliceDeclarations => "        scalar Child\n        optional Child optional\n        text String\n        id Uuid\n        count Int\n        amount Decimal\n        active Bool\n        date Date\n        timestamp DateTime\n        name Name\n        external Imported\n";
    protected override string Declarations => "type Child\n  value String\nconcept Name : String\nimport External.Imported\n";
    void Because() => Result = Compiler.Compile(Source);

    [Fact] void should_allow_forward_top_level_composites_scalars_and_collections() => Result.Success.ShouldBeTrue();
    [Fact] void should_allow_primitives_concepts_and_imported_names_without_diagnostics() => Result.Diagnostics.ShouldBeEmpty();
}
