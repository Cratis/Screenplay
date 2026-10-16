// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_uuid_comparisons;

public class without_uuid_comparisons : given.a_uuid_comparison
{
    ExecutableSemanticModel _protected;
    ExecutableSemanticModel _plain;

    void Because()
    {
        _protected = Bind(Model("note != \"\"")).Value!.Model;
        _plain = Bind("""
            concept MemberId : Uuid
            module M
              feature F
                slice StateChange Write
                  command Update
                    id String identifier
                    member MemberId
                    raw Uuid
                    validate
                      require id != ""
                    produces Recorded
                      for id
                  event Recorded
            """).Value!.Model;
    }

    [Fact] void should_not_promote_uuid_shaped_commands_without_comparisons() => _plain.SemanticVersion.ShouldEqual(SemanticVersion.V2);
    [Fact] void should_keep_the_protected_read_version() => _protected.SemanticVersion.ShouldEqual(SemanticVersion.V25);
    [Fact] void should_keep_the_existing_language_version() => _plain.LanguageVersion.ShouldEqual(LanguageVersion.V2);
}
