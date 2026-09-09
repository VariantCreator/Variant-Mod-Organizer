using DovaInstaller;
using System.Security.Cryptography;

if(args.Length==2&&args[0]=="--inspect-lock-save")
{
 try{var doc=LockSaveEditor.Export(args[1]);Console.WriteLine($"Read OK: {doc.Locks.Count} lock records; generation {doc.Generation}");}catch(Exception ex){Console.WriteLine(ex.ToString());Environment.ExitCode=1;}return;
}
if(args.Length==2&&args[0]=="--test-lock-copy")
{
 var pair=LockSaveEditor.Pair(args[1]);var original=new[]{pair.A,pair.B}.ToDictionary(p=>p,p=>File.ReadAllBytes(p));
 var folder=Path.Combine(Path.GetTempPath(),"DovaSaveCopy-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
 foreach(var item in original){if(!LockSaveFile.Read(item.Value).Write().SequenceEqual(item.Value))throw new Exception("Roundtrip changed bytes");File.WriteAllBytes(Path.Combine(folder,Path.GetFileName(item.Key)),item.Value);}
 var copy=Path.Combine(folder,Path.GetFileName(pair.A));var doc=LockSaveEditor.Export(copy);int gen=doc.Generation;
 doc.Locks[0]=doc.Locks[0] with {RemoveLock=true};LockSaveEditor.ImportDocument(copy,doc);
 var after=LockSaveEditor.Export(copy);if(after.Generation!=gen+1||after.Locks[0].Pin!="")throw new Exception("Clear did not persist");
 var changed=LockSaveFile.Read(File.ReadAllBytes(copy));if(changed.Number("SchemaVersion")!=2)throw new Exception("Schema changed");
 foreach(var item in original)if(!File.ReadAllBytes(item.Key).SequenceEqual(item.Value))throw new Exception("Original changed");
 Console.WriteLine("PASS: actual schema 2 pair roundtrips exactly; isolated edit updates generation and clears selected lock; original downloads unchanged.");return;
}

string suite = Path.Combine(Path.GetTempPath(), "DovaInstallerTests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(suite);
OutputTests.Run(suite);
CrashReportTests.Run(suite);await OrganizerUpdateTests.Run(suite);
byte[] payload = "new-dova-pak-test"u8.ToArray();
string hash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
int passed = 0;
void Check(bool value, string label) { if(!value) throw new Exception(label); passed++; Console.WriteLine("PASS: " + label); }
string Game(string name)
{
 string root=Path.Combine(suite,name,"Icarus");
 foreach(var p in new[]{"Content/Data","Content/Paks","Binaries/Win64","Saved"}) Directory.CreateDirectory(Path.Combine(root,p));
 File.WriteAllText(Path.Combine(root,"Content/Data/data.pak"),"game"); File.WriteAllText(Path.Combine(root,"Binaries/Win64/Icarus-Win64-Shipping.exe"),"game"); File.WriteAllText(Path.Combine(root,"Saved/locks.sav"),"saved-locks"); return root;
}
InstallResult Run(string root, Action<string>? checkpoint=null, string? wantedHash=null) => InstallCore.Apply(root,()=>new MemoryStream(payload),wantedHash??hash,_=>{},checkpoint:checkpoint);
var fresh=Game("fresh");var result=Run(fresh);
Check(InstallCore.Hash(result.Destination)==hash,"fresh install creates Mods and verifies payload");
Check(InstallCore.NormalizeGame(Path.GetDirectoryName(fresh)!)==fresh,"parent folder detected");
Check(File.ReadAllText(Path.Combine(fresh,"Saved/locks.sav"))=="saved-locks","lock save preserved");
var mods=Path.Combine(fresh,"Content/Paks/mods");File.WriteAllText(Path.Combine(mods,"Rada-CheatMenu_P.pak"),"rada");
File.WriteAllText(Path.Combine(mods,"dovaslock_v5n1_P.pak"),"legacy");File.WriteAllText(Path.Combine(mods,"DovaSomethingElse_P.pak"),"other");
var update=Run(fresh);Check(update.Replaced==2,"only canonical and known old Dova PAKs replaced");
Check(File.ReadAllText(Path.Combine(mods,"Rada-CheatMenu_P.pak"))=="rada"&&File.ReadAllText(Path.Combine(mods,"DovaSomethingElse_P.pak"))=="other","Rada and unrelated mods untouched");
Check(File.ReadAllText(Path.Combine(update.Backup,"Content/Paks/mods/dovaslock_v5n1_P.pak"))=="legacy","old PAK backed up outside Paks");
foreach(var stage in new[]{"backed-up","published"})
{
 string root=Game(stage);string dir=Path.Combine(root,"Content/Paks/mods");Directory.CreateDirectory(dir);string old=Path.Combine(dir,"dovaslock_v5n_P.pak");File.WriteAllText(old,"old");
 try { Run(root,point=>{if(point==stage)throw new IOException("injected failure");});throw new Exception("failure not triggered"); } catch(IOException ex) when(ex.Message=="injected failure"){}
 Check(File.ReadAllText(old)=="old"&&!File.Exists(Path.Combine(dir,InstallCore.FileName)),"rollback restores old PAK after "+stage);
}
try { Run(fresh,wantedHash:new string('0',64));throw new Exception("bad hash accepted"); } catch(IOException ex) when(ex.Message.Contains("verified")){}
Check(InstallCore.Hash(result.Destination)==hash,"bad payload does not replace existing installation");
try { InstallCore.Plan(suite);throw new Exception("invalid folder accepted"); } catch(IOException){}
Check(InstallCore.NormalizeGame(suite)==null,"invalid folder rejected");
var running=Game("running");try { InstallCore.Apply(running,()=>new MemoryStream(payload),hash,_=>throw new IOException("running"));throw new Exception("running check ignored"); } catch(IOException ex) when(ex.Message=="running"){}
Check(!Directory.Exists(Path.Combine(running,"DovaLocks-Backups")),"running-game check stops before file changes");
var manage = Game("manage"); var manageMods = Path.Combine(manage,"Content/Paks/mods"); Directory.CreateDirectory(manageMods);
var one = Path.Combine(manageMods,"One.pak"); var two = Path.Combine(manageMods,"Two.PAK");
File.WriteAllText(one,"first"); File.WriteAllText(two,"second"); File.WriteAllText(Path.Combine(manageMods,"notes.txt"),"keep");
var scan = InstallCore.ScanMods(manage);
Check(scan.Count==2,"scan lists only PAK files and handles uppercase extensions");
var removed = InstallCore.RemoveMods(manage,new[]{scan.Single(m=>m.Name=="One.pak")},_=>{});
Check(!File.Exists(one)&&File.Exists(two)&&File.ReadAllText(Path.Combine(removed.Backup,"One.pak"))=="first","selected mod moved to backup; unselected mod untouched");
Check(File.ReadAllText(Path.Combine(manage,"Saved/locks.sav"))=="saved-locks"&&File.Exists(Path.Combine(manageMods,"notes.txt")),"mod removal preserves saves and unrelated files");
var stale = InstallCore.ScanMods(manage); File.WriteAllText(two,"changed-content");
try { InstallCore.RemoveMods(manage,stale,_=>{});throw new Exception("stale accepted"); } catch(IOException ex) when(ex.Message.Contains("changed")){}
Check(File.ReadAllText(two)=="changed-content","changed file rejected before removal");
var outside = Path.Combine(manage,"Content/Paks/base.pak");File.WriteAllText(outside,"base");var info = new FileInfo(outside);
try { InstallCore.RemoveMods(manage,new[]{new ModFile(outside,info.Length,info.LastWriteTimeUtc)},_=>{});throw new Exception("outside accepted"); } catch(IOException ex) when(ex.Message.Contains("Only PAK")){}
Check(File.Exists(outside),"files outside Mods cannot be removed");
File.WriteAllText(one,"first"); scan=InstallCore.ScanMods(manage);
try { InstallCore.RemoveMods(manage,scan,_=>{},_=>throw new IOException("injected"));throw new Exception("failure ignored"); } catch(IOException ex) when(ex.Message=="injected"){}
Check(File.Exists(one)&&File.Exists(two),"failed removal rolls back already moved mods");
try { InstallCore.RemoveMods(manage,scan,_=>throw new IOException("running"));throw new Exception("running ignored"); } catch(IOException ex) when(ex.Message=="running"){}
Check(File.Exists(one)&&File.Exists(two),"running game blocks mod removal");
Check(InstallCore.ScanMods(Game("empty-scan")).Count==0,"scan handles missing Mods folder without creating it");
Console.WriteLine($"{passed} checks passed. Test fixtures: {suite}");

OrganizerTests.Run(suite,Game,Check);
await NetworkTests.Run();

