using System.IO;
using System.Text;
using System.Text.Json;

namespace DovaInstaller;

public sealed record LockPerson(string SteamId, string Name, string Role = "Member", bool Blocked = false);
public sealed record EditableLock(string Id, LockPerson Owner, string Pin, List<LockPerson> Players, bool RemoveLock = false);
public sealed record EditableLocks(int Format, string World, int Generation, List<EditableLock> Locks);

// Preserve the engine header, property GUIDs and unknown payloads. Only the
// supported Dova save fields are rewritten; these files are never text-edited.
public sealed class LockSaveFile
{
 public sealed record Property(string Name, string Type, string? Inner, byte[] Header, int SizeOffset, byte[] Payload);
 public byte[] Prefix = [];
 public byte[] Suffix = [];
 public List<Property> Properties = [];
 public static LockSaveFile Read(byte[] bytes)
 {
  if (bytes.Length > 32 * 1024 * 1024) throw new InvalidDataException("This save is too large to edit here.");
  using var input = new MemoryStream(bytes); using var r = new BinaryReader(input);
  if (r.ReadUInt32() != 0x53415647 || r.ReadInt32() != 2) throw new InvalidDataException("Choose a Dova Locks .sav file.");
  r.ReadInt32(); r.ReadUInt16(); r.ReadUInt16(); r.ReadUInt16(); r.ReadUInt32(); ReadString(r);
  if (r.ReadInt32() != 3) throw new InvalidDataException("This save format is not supported.");
  int versions = r.ReadInt32(); if (versions < 0 || versions > 1000) throw new InvalidDataException("Invalid save header.");
  if (r.ReadBytes(checked(versions * 20)).Length != versions * 20) throw new EndOfStreamException();
  string cls = ReadString(r);
  if (cls != "/Game/Mods/DovaLocks/BP_LockSave.BP_LockSave_C") throw new InvalidDataException("This is not a Dova Locks save.");
  var result = new LockSaveFile { Prefix = bytes[..(int)input.Position] };
  while (true)
  {
   int start = (int)input.Position; string name = ReadString(r);
   if (name == "None") { result.Suffix = bytes[start..]; break; }
   if (result.Properties.Count >= 256 || result.Properties.Any(p => p.Name == name)) throw new InvalidDataException("Invalid save properties.");
   string type = ReadString(r); int sizeOffset = (int)input.Position - start; int size = r.ReadInt32(); int arrayIndex = r.ReadInt32();
   if (size < 0 || size > bytes.Length || arrayIndex != 0) throw new InvalidDataException("Invalid save property length.");
   string? inner = null;
   switch (type)
   {
    case "ArrayProperty": case "SetProperty": inner = ReadString(r); break;
    case "MapProperty": ReadString(r); ReadString(r); break;
    case "StructProperty": ReadString(r); if (r.ReadBytes(16).Length != 16) throw new EndOfStreamException(); break;
    case "ByteProperty": case "EnumProperty": ReadString(r); break;
    case "BoolProperty": r.ReadByte(); break;
    case "IntProperty": case "StrProperty": case "FloatProperty": case "NameProperty": case "Int64Property": break;
    default: throw new InvalidDataException("This save has a newer property type. Nothing was changed.");
   }
   byte guid = r.ReadByte(); if (guid > 1) throw new InvalidDataException("Invalid property header.");
   if (guid == 1 && r.ReadBytes(16).Length != 16) throw new EndOfStreamException();
   byte[] header = bytes[start..(int)input.Position]; byte[] payload = r.ReadBytes(size);
   if (payload.Length != size) throw new EndOfStreamException();
   result.Properties.Add(new(name,type,inner,header,sizeOffset,payload));
  }
  result.Validate(); return result;
 }
 public int Number(string name) { var p = Find(name); if(p.Type!="IntProperty" || p.Payload.Length!=4) throw new InvalidDataException("Invalid number in save."); return BitConverter.ToInt32(p.Payload); }
 public string Text(string name) { var p=Find(name); if(p.Type!="StrProperty")throw new InvalidDataException("Invalid text in save.");using var r=new BinaryReader(new MemoryStream(p.Payload));return ReadString(r); }
 public List<string> Array(string name)
 {
  var p=Properties.SingleOrDefault(p=>p.Name.Equals(name,StringComparison.OrdinalIgnoreCase)); if(p==null)return [];
  if(p.Type!="ArrayProperty" || p.Inner!="StrProperty")throw new InvalidDataException("Invalid list in save.");
  using var r=new BinaryReader(new MemoryStream(p.Payload));int n=r.ReadInt32();if(n<0||n>100000)throw new InvalidDataException("Invalid list length.");
  var list=new List<string>();for(int i=0;i<n;i++)list.Add(ReadString(r));if(r.BaseStream.Position!=r.BaseStream.Length)throw new InvalidDataException("Invalid list contents.");return list;
 }
 Property Find(string name)=>Properties.SingleOrDefault(p=>p.Name.Equals(name,StringComparison.OrdinalIgnoreCase))??throw new InvalidDataException("Missing save field: "+name);
 public void SetArray(string name,List<string> values)
 {
  using var m=new MemoryStream();using var b=new BinaryWriter(m);b.Write(values.Count);foreach(var s in values)WriteString(b,s);Replace(name,m.ToArray());
 }
 public void SetNumber(string name,int value)=>Replace(name,BitConverter.GetBytes(value));
 void Replace(string name,byte[] payload){int i=Properties.FindIndex(p=>p.Name.Equals(name,StringComparison.OrdinalIgnoreCase));if(i<0)throw new InvalidDataException("This older save needs to be opened in the game before editing: "+name);Properties[i]=Properties[i] with {Payload=payload};}
 public byte[] Write()
 {
  Validate();using var m=new MemoryStream();m.Write(Prefix);foreach(var p in Properties){var h=p.Header.ToArray();BitConverter.GetBytes(p.Payload.Length).CopyTo(h,p.SizeOffset);m.Write(h);m.Write(p.Payload);}m.Write(Suffix);return m.ToArray();
 }
 public static readonly string[] RecordFields=["Owners","PINs","Names","Access","BaseFlags","Associates","CoOwners","PeerIDs","PeerLabels","Revoked","RevokedCoOwners"];
 public void Validate()
 {
  if(Number("SchemaVersion") is not (1 or 2))throw new InvalidDataException("This save uses lock format "+Number("SchemaVersion")+". The editor currently supports formats 1 and 2.");
  if(Number("Generation")<0)throw new InvalidDataException("The save has an invalid generation.");
  if(string.IsNullOrWhiteSpace(Text("ProspectID")))throw new InvalidDataException("The save has no world identifier.");
  var ids=Array("RecordIDs");if(ids.Distinct().Count()!=ids.Count)throw new InvalidDataException("Duplicate lock records.");
  foreach(var n in RecordFields) {var a=Array(n);if(a.Count!=ids.Count)throw new InvalidDataException("Incomplete lock records: "+n+" has "+a.Count+" entries for "+ids.Count+" locks.");}
  if(Array("ObjectKeys").Count!=Array("ObjectRecords").Count)throw new InvalidDataException("Incomplete object links.");
  if(Array("ObjectRecords").Any(id=>!ids.Contains(id)))throw new InvalidDataException("Unknown linked lock.");
 }
 static string ReadString(BinaryReader r)
 {
  int length=r.ReadInt32();if(length==0)return "";if(length==int.MinValue||Math.Abs(length)>1000000)throw new InvalidDataException("Invalid text length.");
  int count=checked(Math.Abs(length)*(length<0?2:1));byte[] raw=r.ReadBytes(count);if(raw.Length!=count)throw new EndOfStreamException();
  if(raw[^1]!=0 || (length<0&&raw[^2]!=0))throw new InvalidDataException("Invalid text ending.");
  return (length<0?Encoding.Unicode:new UTF8Encoding(false,true)).GetString(raw,0,count-(length<0?2:1));
 }
 static void WriteString(BinaryWriter w,string text){if(text.Any(c=>c>127)){var b=Encoding.Unicode.GetBytes(text+'\0');w.Write(-b.Length/2);w.Write(b);}else{var b=Encoding.UTF8.GetBytes(text+'\0');w.Write(b.Length);w.Write(b);}}
}

public static class LockSaveEditor
{
 public static readonly JsonSerializerOptions JsonOptions=new(){WriteIndented=true,PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true};
 static List<string> Split(string s)=>s.Split('|',StringSplitOptions.RemoveEmptyEntries).ToList();
 static string Join(IEnumerable<string> s)=>string.Join('|',s.Distinct());
 public static (string A,string B) Pair(string file)
 {
  file=Path.GetFullPath(file);string name=Path.GetFileName(file);
  if(!name.StartsWith("DovaLocks_",StringComparison.Ordinal)||!(name.EndsWith("_A.sav",StringComparison.OrdinalIgnoreCase)||name.EndsWith("_B.sav",StringComparison.OrdinalIgnoreCase)))throw new InvalidDataException("Select a DovaLocks … A.sav or B.sav file.");
  string prefix=file[..^6];return(prefix+"_A.sav",prefix+"_B.sav");
 }
 public static LockSaveFile Latest(string file)
 {
  var (a,b)=Pair(file);var saves=new[]{a,b}.Where(File.Exists).Select(p=>LockSaveFile.Read(File.ReadAllBytes(p))).ToList();
  if(saves.Count==0)throw new FileNotFoundException("No lock save found.");if(saves.Select(s=>s.Text("ProspectID")).Distinct().Count()!=1)throw new InvalidDataException("These A/B files belong to different worlds.");
  return saves.OrderByDescending(s=>s.Number("Generation")).First();
 }
 public static EditableLocks Export(string file)
 {
  var save=Latest(file);var ids=save.Array("RecordIDs");var fields=LockSaveFile.RecordFields.ToDictionary(n=>n,n=>save.Array(n));var records=new List<EditableLock>();
  for(int i=0;i<ids.Count;i++)
  {
   List<string> Get(string n)=>Split(fields[n][i]);var peerIds=Get("PeerIDs");var labels=fields["PeerLabels"][i].Split('|');
   string Name(string id){int ix=peerIds.IndexOf(id);return ix>=0&&ix<labels.Length?labels[ix]:"";}
   var blocked=Get("Revoked").Concat(Get("RevokedCoOwners")).ToHashSet();var associates=Get("Associates");var co=Get("CoOwners");
   var people=Get("Access").Concat(co).Concat(associates).Concat(peerIds).Concat(blocked).Distinct().Where(id=>id!=fields["Owners"][i]).Select(id=>new LockPerson(id,Name(id),co.Contains(id)?"Co-owner":associates.Contains(id)?"Associate":"Member",blocked.Contains(id))).ToList();
   records.Add(new(ids[i],new(fields["Owners"][i],fields["Names"][i],"Owner"),fields["PINs"][i],people));
  }
  return new(1,save.Text("ProspectID"),save.Number("Generation"),records);
 }
 public static byte[] ApplyEdits(LockSaveFile save,EditableLocks edited)
 {
  if(edited.Format!=1||edited.World!=save.Text("ProspectID")||edited.Generation!=save.Number("Generation"))throw new InvalidDataException("The save changed since export. Export a fresh copy before editing.");
  var ids=save.Array("RecordIDs");if(edited.Locks.Count!=ids.Count||edited.Locks.Select(l=>l.Id).Distinct().Count()!=ids.Count||edited.Locks.Any(l=>!ids.Contains(l.Id)))throw new InvalidDataException("Keep each lock record and its id. Use removeLock to clear a lock.");
  var fields=LockSaveFile.RecordFields.ToDictionary(n=>n,n=>save.Array(n));
  foreach(var record in edited.Locks)
  {
   int i=ids.IndexOf(record.Id);void Set(string n,string v)=>fields[n][i]=v;
   if(record.RemoveLock){foreach(var n in LockSaveFile.RecordFields)Set(n,n=="BaseFlags"?"false":"");continue;}
   if(record.Pin!="" && (record.Pin.Length!=4||record.Pin.Any(c=>c<'0'||c>'9')||string.IsNullOrWhiteSpace(record.Owner.SteamId)))throw new InvalidDataException("A locked record needs a four-digit PIN and an owner. Use removeLock to clear it.");
   foreach(var person in record.Players.Append(record.Owner))if(person.SteamId.Any(c=>c=='|'||char.IsControl(c))||person.Name.Any(c=>c=='|'||char.IsControl(c))||person.SteamId.Length>100||person.Name.Length>200)throw new InvalidDataException("A player name or ID contains unsupported characters.");
   if(record.Players.Any(p=>string.IsNullOrWhiteSpace(p.SteamId)||p.SteamId==record.Owner.SteamId||!new[]{"Associate","Member","Co-owner"}.Contains(p.Role))||record.Players.Select(p=>p.SteamId).Distinct().Count()!=record.Players.Count)throw new InvalidDataException("Each player needs a unique ID and an Associate, Member or Co-owner role.");
   var removed=Split(fields["Access"][i]).Concat(Split(fields["CoOwners"][i])).Concat(Split(fields["Associates"][i])).Where(id=>record.Players.All(p=>p.SteamId!=id)&&id!=record.Owner.SteamId);
   var blocked=Split(fields["Revoked"][i]).Concat(Split(fields["RevokedCoOwners"][i])).Concat(removed).Concat(record.Players.Where(p=>p.Blocked).Select(p=>p.SteamId)).Except(record.Players.Where(p=>!p.Blocked).Select(p=>p.SteamId)).ToList();
   Set("Owners",record.Owner.SteamId);Set("Names",record.Owner.Name);Set("PINs",record.Pin);
   Set("Access",Join(record.Players.Where(p=>!p.Blocked).Select(p=>p.SteamId)));Set("Associates",Join(record.Players.Where(p=>!p.Blocked&&p.Role=="Associate").Select(p=>p.SteamId)));Set("CoOwners",Join(record.Players.Where(p=>!p.Blocked&&p.Role=="Co-owner").Select(p=>p.SteamId)));
   Set("PeerIDs",string.Join('|',record.Players.Select(p=>p.SteamId)));Set("PeerLabels",string.Join('|',record.Players.Select(p=>string.IsNullOrEmpty(p.Name)?p.SteamId:p.Name)));Set("Revoked",Join(blocked));Set("RevokedCoOwners","");
  }
  foreach(var field in fields)save.SetArray(field.Key,field.Value);save.SetNumber("Generation",checked(edited.Generation+1));return save.Write();
 }
 public static string Import(string file,string json)
 {
  var edited=JsonSerializer.Deserialize<EditableLocks>(File.ReadAllText(json),JsonOptions)??throw new InvalidDataException("Empty edit file.");
  return ImportDocument(file,edited);
 }
 public static string ImportDocument(string file,EditableLocks edited)
 {
  var pair=Pair(file);var before=new[]{pair.A,pair.B}.ToDictionary(p=>p,p=>File.Exists(p)?File.ReadAllBytes(p):null);
  byte[] updated=ApplyEdits(Latest(file),edited);LockSaveFile.Read(updated);
  string folder=Path.Combine(Path.GetDirectoryName(file)!,"DovaLocks-Backups",DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..6]);Directory.CreateDirectory(folder);
  foreach(var item in before)if(item.Value!=null)File.WriteAllBytes(Path.Combine(folder,Path.GetFileName(item.Key)),item.Value);
  var written=new List<string>();
  try{foreach(var path in before.Keys){if(File.Exists(path)!=(before[path]!=null)||(File.Exists(path)&&!File.ReadAllBytes(path).SequenceEqual(before[path]??[])))throw new IOException("The save changed while editing. Stop the server and try again.");string temp=path+".editing-"+Guid.NewGuid().ToString("N");try{File.WriteAllBytes(temp,updated);File.Move(temp,path,true);written.Add(path);}finally{if(File.Exists(temp))File.Delete(temp);}}}
  catch{foreach(var path in written){if(before[path] is byte[] old)File.WriteAllBytes(path,old);else File.Delete(path);}throw;}
  return folder;
 }
}
