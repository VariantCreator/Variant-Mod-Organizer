using DovaInstaller;
using System.IO.Compression;
using System.Text.Json;
static class OrganizerTests
{
 public static void Run(string suite,Func<string,string> game,Action<bool,string> check)
 {
  var steamGame=game("Steam/steamapps/common/Icarus");var apps=Directory.GetParent(steamGame)!.Parent!.Parent!.FullName;string manifest=Path.Combine(apps,"appmanifest_1149460.acf");
  File.WriteAllText(manifest,"\"AppState\" { \"appid\" \"1149460\" \"installdir\" \"Icarus\" }");
  check(OrganizerCore.SteamLaunchTarget(steamGame)=="steam://rungameid/1149460","launch uses Steam account context instead of shipping executable");
  File.WriteAllText(manifest,"\"AppState\" { \"appid\" \"1149460\" \"installdir\" \"DifferentGame\" }");
  try{OrganizerCore.SteamLaunchTarget(steamGame);throw new Exception("wrong Steam folder accepted");}catch(IOException){}
  check(true,"Steam launch rejects a folder that differs from its installation manifest");
  var root=game("organizer");var mods=Path.Combine(root,"Content/Paks/mods");Directory.CreateDirectory(mods);
  File.WriteAllText(Path.Combine(mods,"A.pak"),"original");
  OrganizerCore.SetMode(root,true,_=>{});check(!Directory.Exists(mods)&&OrganizerCore.IsVanilla(root),"vanilla moves mods outside Paks");
  OrganizerCore.SetMode(root,true,_=>{});OrganizerCore.SetMode(root,false,_=>{});check(File.ReadAllText(Path.Combine(mods,"A.pak"))=="original","repeated mode switch preserves mods");
  try{OrganizerCore.SetMode(root,true,_=>{},_=>throw new IOException("test"));throw new Exception("failure ignored");}catch(IOException){}
  check(File.Exists(Path.Combine(mods,"A.pak"))&&!OrganizerCore.IsVanilla(root),"failed profile switch restores active mods");
  OrganizerCore.SetMode(root,true,_=>{});Directory.CreateDirectory(mods);File.WriteAllText(Path.Combine(mods,"New.pak"),"new");
  try{OrganizerCore.SetMode(root,false,_=>{});throw new Exception("collision ignored");}catch(IOException){}
  check(File.Exists(Path.Combine(mods,"New.pak"))&&OrganizerCore.IsVanilla(root),"profile collision never overwrites new mods");
  File.Delete(Path.Combine(mods,"New.pak"));Directory.Delete(mods);OrganizerCore.SetMode(root,false,_=>{});
  var pak=Path.Combine(suite,"A.pak");File.WriteAllBytes(pak,[1,2,3,0xe1,0x12,0x6f,0x5a,0,0]);
  string backup=OrganizerCore.Import(root,pak,_=>{});check(File.ReadAllText(Path.Combine(backup,"A.pak"))=="original","import backs up replaced mod");
  var zip=Path.Combine(suite,"mods.zip");using(var z=ZipFile.Open(zip,ZipArchiveMode.Create)){z.CreateEntryFromFile(pak,"Folder/B.pak");z.CreateEntry("README.txt");}
  OrganizerCore.Import(root,zip,_=>{});check(File.Exists(Path.Combine(mods,"B.pak")),"ZIP import installs nested PAK and ignores documentation");
  var unsafeZip=Path.Combine(suite,"unsafe.zip");using(var z=ZipFile.Open(unsafeZip,ZipArchiveMode.Create))z.CreateEntryFromFile(pak,"../Escape.pak");
  try{OrganizerCore.Import(root,unsafeZip,_=>{});throw new Exception("unsafe accepted");}catch(IOException){}
  check(!File.Exists(Path.Combine(root,"Content/Paks/Escape.pak")),"ZIP traversal rejected");
  File.WriteAllText(Path.Combine(mods,"A.pak"),"rollback");try{OrganizerCore.Import(root,pak,_=>{},_=>throw new IOException("test"));throw new Exception("failure ignored");}catch(IOException){}
  check(File.ReadAllText(Path.Combine(mods,"A.pak"))=="rollback","import failure restores previous mod");
  var listed=InstallCore.ScanMods(root).ToArray();var original=listed.ToDictionary(f=>f.Name,f=>File.ReadAllBytes(f.Path));
  OrganizerCore.SetEnabled(root,listed,false,_=>{});
  check(InstallCore.ScanMods(root).Count==0&&OrganizerCore.ScanInactive(root).Count==listed.Length,"disable moves selected mods outside game Paks");
  OrganizerCore.SetMode(root,true,_=>{});OrganizerCore.SetMode(root,false,_=>{});
  check(InstallCore.ScanMods(root).Count==0,"vanilla and modded switching keeps individually disabled mods disabled");
  var parked=OrganizerCore.ScanInactive(root).ToArray();
  try{OrganizerCore.SetEnabled(root,parked,true,_=>{},_=>throw new IOException("test"));throw new Exception("move failure ignored");}catch(IOException){}
  check(OrganizerCore.ScanInactive(root).Count==listed.Length&&InstallCore.ScanMods(root).Count==0,"failed enable rolls back all moved files");
  OrganizerCore.SetEnabled(root,OrganizerCore.ScanInactive(root).ToArray(),true,_=>{});
  check(InstallCore.ScanMods(root).All(f=>File.ReadAllBytes(f.Path).SequenceEqual(original[f.Name])),"reenabling preserves exact mod bytes");
  Directory.CreateDirectory(OrganizerCore.InactiveMods(root));File.WriteAllText(Path.Combine(OrganizerCore.InactiveMods(root),listed[0].Name),"other version");
  try{OrganizerCore.SetEnabled(root,InstallCore.ScanMods(root).ToArray(),false,_=>{});throw new Exception("duplicate replaced");}catch(IOException){}
  check(InstallCore.ScanMods(root).Count==listed.Length,"duplicate on other side refuses move without replacing files");
  var saves=Path.Combine(suite,"saves");Directory.CreateDirectory(saves);
  foreach(var suffix in new[]{"A","B"})File.Copy(Path.Combine(AppContext.BaseDirectory,"Fixtures","DovaLocks_ADMIN_FIXTURE_"+suffix+".sav"),Path.Combine(saves,"DovaLocks_ADMIN_FIXTURE_"+suffix+".sav"));
  string a=Path.Combine(saves,"DovaLocks_ADMIN_FIXTURE_A.sav"),b=Path.Combine(saves,"DovaLocks_ADMIN_FIXTURE_B.sav");
  var bytes=File.ReadAllBytes(a);check(LockSaveFile.Read(bytes).Write().SequenceEqual(bytes),"Unreal-generated save roundtrips without any byte changes");
  var doc=LockSaveEditor.Export(a);check(doc.Generation==11&&doc.Locks[0].Owner.Name=="Example Owner","editor reads latest A/B generation and names");
  string removed=doc.Locks[0].Players[0].SteamId;doc.Locks[0].Players.RemoveAt(0);var edit=Path.Combine(saves,"edit.json");File.WriteAllText(edit,JsonSerializer.Serialize(doc,LockSaveEditor.JsonOptions));
  string saveBackup=LockSaveEditor.Import(a,edit);var after=LockSaveFile.Read(File.ReadAllBytes(a));
  check(after.Number("Generation")==12&&File.ReadAllBytes(a).SequenceEqual(File.ReadAllBytes(b)),"both save slots updated together");
  check(after.Array("Revoked")[0].Contains(removed)&&!after.Array("Access")[0].Contains(removed),"removed player blocked from re-entering old PIN");
  check(File.ReadAllBytes(Path.Combine(saveBackup,Path.GetFileName(a))).SequenceEqual(bytes),"original save backed up exactly");
  try{LockSaveEditor.Import(a,edit);throw new Exception("stale accepted");}catch(InvalidDataException){}
  check(LockSaveEditor.Export(a).Generation==12,"stale edits rejected");

 }
}

