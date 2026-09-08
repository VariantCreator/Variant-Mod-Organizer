using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
namespace DovaInstaller;

public static class OrganizerCore
{
 public static string InactiveMods(string root)=>Path.Combine(Storage(root),"LibraryDisabled");
 public static IReadOnlyList<ModFile> ScanInactive(string root)
 {
  string folder=InactiveMods(root);Safe(InstallCore.Plan(root).Root,folder);
  return !Directory.Exists(folder)?Array.Empty<ModFile>():Directory.EnumerateFiles(folder,"*.pak").Select(p=>new FileInfo(p)).Where(f=>(f.Attributes&FileAttributes.ReparsePoint)==0).Select(f=>new ModFile(f.FullName,f.Length,f.LastWriteTimeUtc)).OrderBy(f=>f.Name,StringComparer.OrdinalIgnoreCase).ToArray();
 }
 public static void SetEnabled(string root,IReadOnlyList<ModFile> selected,bool enabled,Action<string>? closedCheck=null,Action<int>? checkpoint=null)
 {
  var plan=InstallCore.Plan(root);(closedCheck??InstallCore.EnsureGameClosed)(plan.Root);using var guard=Guard(plan.Root);
  if(IsVanilla(root))throw new IOException("Return to modded mode before changing individual mods.");
  string from=enabled?InactiveMods(root):plan.Mods,to=enabled?plan.Mods:InactiveMods(root);
  MoveSelection(plan.Root,selected,from,to,checkpoint);
 }
 static void MoveSelection(string root,IReadOnlyList<ModFile> selected,string from,string to,Action<int>? checkpoint=null)
 {
  Safe(root,from);Safe(root,to);var files=selected.DistinctBy(m=>m.Path,StringComparer.OrdinalIgnoreCase).ToArray();if(files.Length==0)throw new IOException("Select a mod first.");
  foreach(var f in files){Safe(root,f.Path);var info=new FileInfo(f.Path);if(!string.Equals(Path.GetDirectoryName(Path.GetFullPath(f.Path)),Path.GetFullPath(from),StringComparison.OrdinalIgnoreCase)||!info.Exists||info.Length!=f.Length||info.LastWriteTimeUtc!=f.Modified||!info.Extension.Equals(".pak",StringComparison.OrdinalIgnoreCase))throw new IOException("The mod list changed. Scan again before moving files.");if(File.Exists(Path.Combine(to,f.Name)))throw new IOException(f.Name+" is already on the other side. Nothing was replaced.");}
  Directory.CreateDirectory(to);var moved=new List<ModFile>();try{foreach(var f in files){File.Move(f.Path,Path.Combine(to,f.Name));moved.Add(f);checkpoint?.Invoke(moved.Count);}}catch{foreach(var f in moved.AsEnumerable().Reverse())File.Move(Path.Combine(to,f.Name),f.Path);throw;}
 }
 public static string RemoveInactive(string root,IReadOnlyList<ModFile> selected)
 {
  var plan=InstallCore.Plan(root);InstallCore.EnsureGameClosed(plan.Root);using var guard=Guard(plan.Root);string backup=Path.Combine(plan.Root,"DovaLocks-Backups","removed-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..6]);MoveSelection(plan.Root,selected,InactiveMods(root),backup);return backup;
 }
 public static string Storage(string root) => Path.Combine(InstallCore.Plan(root).Root,"Variant-Mod-Organizer");
 static void Safe(string root,string path)
 {
  root=Path.GetFullPath(root);path=Path.GetFullPath(path);
  if(!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("The file is outside this game folder.");
  for(string? p=path;p!=null&&!p.Equals(root,StringComparison.OrdinalIgnoreCase);p=Path.GetDirectoryName(p))
   if((File.Exists(p)||Directory.Exists(p))&&(File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new IOException("Choose the actual folder instead of a linked folder.");
 }
 static FileStream Guard(string root)
 {
  var dir=Path.Combine(root,"DovaLocks-Backups");Safe(root,dir);Directory.CreateDirectory(dir);
  return new FileStream(Path.Combine(dir,".install-lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
 }
 public static bool IsVanilla(string root) => Directory.Exists(Path.Combine(Storage(root),"Disabled"));
 public static void SetMode(string selection,bool vanilla,Action<string>? closedCheck=null,Action<int>? checkpoint=null)
 {
  var plan=InstallCore.Plan(selection);(closedCheck??InstallCore.EnsureGameClosed)(plan.Root);
  using var guard=Guard(plan.Root);var store=Storage(plan.Root);Safe(plan.Root,store);
  var disabled=Path.Combine(store,"Disabled");Safe(plan.Root,disabled);
  var paks=Path.Combine(plan.Root,"Content","Paks");
  // Unexpected loose PAKs cannot be silently treated as original game content.
  if(vanilla && Directory.EnumerateFiles(paks,"*.pak").Any(p=>!Regex.IsMatch(Path.GetFileName(p),@"^pakchunk\d+(?:_s\d+)?-WindowsNoEditor\.pak$",RegexOptions.IgnoreCase)))throw new IOException("There are extra PAK files directly in Paks. Move your mods into the mods folder before launching vanilla.");
  if(vanilla && Directory.EnumerateDirectories(paks).Any(p=>!new[]{"mods","~mods"}.Contains(Path.GetFileName(p).ToLowerInvariant())))throw new IOException("There are other folders in Paks. Move their mods into the mods folder before launching vanilla.");
  bool wasVanilla=Directory.Exists(disabled);var pairs=new List<(string From,string To)>();
  if(vanilla)
  {
   foreach(var dir in Directory.EnumerateDirectories(paks).Where(p=>new[]{"mods","~mods"}.Contains(Path.GetFileName(p).ToLowerInvariant())))
   {
    Safe(plan.Root,dir);foreach(var child in Directory.EnumerateFileSystemEntries(dir,"*",new EnumerationOptions{RecurseSubdirectories=true,AttributesToSkip=0}))Safe(plan.Root,child);
    pairs.Add((dir,Path.Combine(disabled,Path.GetFileName(dir))));
   }
  }
  else if(wasVanilla)foreach(var dir in Directory.EnumerateDirectories(disabled))
  {
   if(!new[]{"mods","~mods"}.Contains(Path.GetFileName(dir).ToLowerInvariant()))throw new IOException("The stored mod folder has changed. Restore it manually before continuing.");
   Safe(plan.Root,dir);pairs.Add((dir,Path.Combine(paks,Path.GetFileName(dir))));
  }
  foreach(var pair in pairs)if(Directory.Exists(pair.To)||File.Exists(pair.To))throw new IOException("Mods exist in both the active and stored folders. Open those folders and keep the versions you want before switching modes.");
  if(vanilla)Directory.CreateDirectory(disabled);
  var moved=new List<(string From,string To)>();
  try
  {
   foreach(var pair in pairs){Directory.Move(pair.From,pair.To);moved.Add(pair);checkpoint?.Invoke(moved.Count);}
   if(!vanilla&&wasVanilla)Directory.Delete(disabled);
  }
  catch
  {
   foreach(var pair in moved.AsEnumerable().Reverse())Directory.Move(pair.To,pair.From);
   if(vanilla&&!wasVanilla&&Directory.Exists(disabled)&&!Directory.EnumerateFileSystemEntries(disabled).Any())Directory.Delete(disabled);
   throw;
  }
 }
 public static string GameExecutable(string root)
 {
  var plan=InstallCore.Plan(root);var exe=Path.Combine(plan.Root,"Binaries","Win64","Icarus-Win64-Shipping.exe");
  if(!File.Exists(exe))throw new IOException("Choose the ICARUS game installation to launch. This may be a dedicated server folder.");
  return exe;
 }
 public static string SteamLaunchTarget(string root)
 {
  GameExecutable(root);
  var full=InstallCore.Plan(root).Root;
  var install=Directory.GetParent(full)!;
  var common=install.Parent;
  var apps=common?.Parent;
  if(common?.Name!="common"||apps?.Name!="steamapps")throw new IOException("Choose the ICARUS installation in your Steam library before launching.");
  string manifest=Path.Combine(apps.FullName,"appmanifest_1149460.acf");
  if(!File.Exists(manifest))throw new IOException("Steam doesn't list ICARUS in this library. Open Steam and check its installed location.");
  string data=File.ReadAllText(manifest);
  string Field(string name)=>Regex.Match(data,"\""+name+"\"\\s+\"([^\"]+)\"").Groups[1].Value;
  if(Field("appid")!="1149460"||!string.Equals(Field("installdir"),install.Name,StringComparison.OrdinalIgnoreCase))throw new IOException("This folder doesn't match Steam's ICARUS installation. Choose the folder shown in Steam.");
  return "steam://rungameid/1149460";
 }
 public static string Import(string root,string source,Action<string>? closedCheck=null,Action<int>? checkpoint=null)
 {
  var plan=InstallCore.Plan(root);(closedCheck??InstallCore.EnsureGameClosed)(plan.Root);
  if(IsVanilla(root))throw new IOException("Switch back to Modded before adding mods.");
  using var guard=Guard(plan.Root);var store=Storage(root);Safe(plan.Root,store);Directory.CreateDirectory(store);
  string stage=Path.Combine(store,"Import-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
  var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  void Add(string name,Stream input,long length)
  {
   if(length<=0||length>512L*1024*1024||names.Count>=100||!names.Add(name))throw new IOException("This import has duplicate files or is too large. Import the PAK files separately.");
   if(name.IndexOfAny(Path.GetInvalidFileNameChars())>=0||name.EndsWith(' ')||name.EndsWith('.'))throw new IOException("A mod has an unsupported filename.");
   using var output=File.Create(Path.Combine(stage,name));var buffer=new byte[81920];long written=0;int n;
   while((n=input.Read(buffer))>0){written+=n;if(written>length)throw new IOException("The archive size does not match its contents.");output.Write(buffer,0,n);}
   if(written!=length)throw new IOException("A mod file is incomplete.");
  }
  var moved=new List<(string From,string To)>();var published=new List<string>();
  string backup=Path.Combine(store,"Backups",DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..8]);
  try
  {
   if(Path.GetExtension(source).Equals(".pak",StringComparison.OrdinalIgnoreCase)){using var f=File.OpenRead(source);Add(Path.GetFileName(source),f,f.Length);}
   else if(Path.GetExtension(source).Equals(".zip",StringComparison.OrdinalIgnoreCase))
   {
    using var zip=ZipFile.OpenRead(source);long total=0;
    foreach(var entry in zip.Entries)
    {
     var parts=entry.FullName.Replace('\\','/').Split('/');
     if(parts.Any(p=>p==".."||p.Contains(':'))||entry.FullName.StartsWith('/')||((entry.ExternalAttributes>>16)&0xf000)==0xa000)throw new IOException("This archive contains unsafe paths. Import the PAK directly.");
     if(!entry.Name.EndsWith(".pak",StringComparison.OrdinalIgnoreCase))continue;
     total=checked(total+entry.Length);if(total>1024L*1024*1024)throw new IOException("Import up to 1 GB at a time.");
     using var f=entry.Open();Add(parts[^1],f,entry.Length);
    }
   }
   else throw new IOException("Choose a PAK file or a ZIP containing PAK files.");
   if(names.Count==0)throw new IOException("No PAK files were found in this ZIP.");
   // Unreal PAK magic is in the footer; a renamed EXE is not a mod.
   foreach(var name in names){using var f=File.OpenRead(Path.Combine(stage,name));f.Seek(Math.Max(0,f.Length-4096),SeekOrigin.Begin);var tail=new byte[(int)(f.Length-f.Position)];f.ReadExactly(tail);if(tail.AsSpan().IndexOf(new byte[]{0xe1,0x12,0x6f,0x5a})<0)throw new IOException(name+" is not a recognized Unreal PAK file.");}
   (closedCheck??InstallCore.EnsureGameClosed)(plan.Root);Safe(plan.Root,plan.Mods);Safe(plan.Root,backup);
   Directory.CreateDirectory(plan.Mods);Directory.CreateDirectory(backup);
   foreach(var name in names)
   {
    var dest=Path.Combine(plan.Mods,name);Safe(plan.Root,dest);
    if(File.Exists(dest)){var old=Path.Combine(backup,name);File.Move(dest,old);moved.Add((dest,old));}
    File.Move(Path.Combine(stage,name),dest);published.Add(dest);checkpoint?.Invoke(published.Count);
   }
   return backup;
  }
  catch {foreach(var p in published)File.Delete(p);foreach(var p in moved.AsEnumerable().Reverse())File.Move(p.To,p.From);throw;}
  finally {if(Directory.Exists(stage))Directory.Delete(stage,true);}
 }
}
