// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_Compatibility;

public class when_inspecting_workspace_transaction_compatibility : Specification
{
    [Fact] void should_keep_stale_workspace_revision_at_zero() => ((int)WorkspaceConflictKind.StaleWorkspaceRevision).ShouldEqual(0);
    [Fact] void should_keep_stale_catalog_revision_at_one() => ((int)WorkspaceConflictKind.StaleCatalogRevision).ShouldEqual(1);
    [Fact] void should_keep_invalid_operation_at_two() => ((int)WorkspaceConflictKind.InvalidOperation).ShouldEqual(2);
    [Fact] void should_keep_portable_path_collision_at_three() => ((int)WorkspaceConflictKind.PortablePathCollision).ShouldEqual(3);
    [Fact] void should_keep_compilation_failed_at_four() => ((int)WorkspaceConflictKind.CompilationFailed).ShouldEqual(4);
    [Fact] void should_keep_invalid_identity_migration_at_five() => ((int)WorkspaceConflictKind.InvalidIdentityMigration).ShouldEqual(5);
    [Fact] void should_add_semantic_id_not_found_at_six() => ((int)WorkspaceConflictKind.SemanticIdNotFound).ShouldEqual(6);
    [Fact] void should_add_unsupported_semantic_field_at_seven() => ((int)WorkspaceConflictKind.UnsupportedSemanticField).ShouldEqual(7);
    [Fact] void should_add_semantic_field_value_drift_at_eight() => ((int)WorkspaceConflictKind.SemanticFieldValueDrift).ShouldEqual(8);
    [Fact] void should_add_multi_owner_semantic_edit_at_nine() => ((int)WorkspaceConflictKind.MultiOwnerSemanticEdit).ShouldEqual(9);
    [Fact] void should_define_update_slice_description_as_a_workspace_operation() => typeof(UpdateSliceDescription).IsSubclassOf(typeof(WorkspaceOperation)).ShouldBeTrue();
    [Fact] void should_expose_the_semantic_id_property() => typeof(UpdateSliceDescription).GetProperty(nameof(UpdateSliceDescription.SemanticId)).ShouldNotBeNull();
    [Fact] void should_expose_the_semantic_id_property_type() => typeof(UpdateSliceDescription).GetProperty(nameof(UpdateSliceDescription.SemanticId))!.PropertyType.ShouldEqual(typeof(SemanticId));
    [Fact] void should_expose_the_expected_current_description_property() => typeof(UpdateSliceDescription).GetProperty(nameof(UpdateSliceDescription.ExpectedCurrentDescription))!.PropertyType.ShouldEqual(typeof(string));
    [Fact] void should_expose_the_new_description_property() => typeof(UpdateSliceDescription).GetProperty(nameof(UpdateSliceDescription.NewDescription))!.PropertyType.ShouldEqual(typeof(string));
}
