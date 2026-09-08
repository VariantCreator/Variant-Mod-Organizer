import unreal
cls = unreal.EditorAssetLibrary.load_blueprint_class('/Game/Mods/DovaLocks/BP_LockSave')
save = unreal.GameplayStatics.create_save_game_object(cls)
for key, value in dict(SchemaVersion=1, Generation=10, ProspectID='ADMIN_FIXTURE', RecordIDs=['lock-one'], Owners=['76561198000000001'], PINs=['0427'], Names=['Example Owner'], Access=['76561198000000002|76561198000000003'], BaseFlags=['1'], ObjectKeys=['example-object'], ObjectRecords=['lock-one'], Associates=['76561198000000002'], CoOwners=['76561198000000003'], PeerIDs=['76561198000000002|76561198000000003'], PeerLabels=['Example Friend|Example Co-owner'], Revoked=[''], RevokedCoOwners=['']).items():
    save.set_editor_property(key,value)
assert unreal.GameplayStatics.save_game_to_slot(save,'DovaLocks_ADMIN_FIXTURE_A',0)
save.set_editor_property('Generation',11)
assert unreal.GameplayStatics.save_game_to_slot(save,'DovaLocks_ADMIN_FIXTURE_B',0)
unreal.log('ADMIN_FIXTURE_SAVED '+unreal.Paths.project_saved_dir())
