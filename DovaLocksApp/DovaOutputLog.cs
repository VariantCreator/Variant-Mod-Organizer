using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DovaInstaller;

// Reads a shared game log without changing it. Only our fixed, non-player event
// messages are copied. A crash marker records that the game reported a failure,
// not that Dova Locks caused it. Keep the original crash folder for investigation.
public sealed class DovaOutputLog
{
 public const int MaxBytes = 1024 * 1024;
 readonly string source, destination;
 long offset;
 string pending = "";
 DateTime creation;
 static readonly Regex Event = new(@"DovaOutPut 1\.[0-9A-Za-z.\-]+ \[(server|client)\] [a-z ]+ code=-?\d+", RegexOptions.CultureInvariant);
 public string Destination => destination;
 public DovaOutputLog(string source, string folder, string role)
 {
  if(role is not ("Client" or "Server")) throw new ArgumentException("Choose Client or Server.");
  this.source=Path.GetFullPath(source);
  destination=Path.Combine(Path.GetFullPath(folder),$"DovaOutPut.{role}.log");
  if(string.Equals(this.source,destination,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Output must be separate from the game log.");
 }
 public int Poll()
 {
  if(!File.Exists(source))return 0;
  var born=File.GetCreationTimeUtc(source);
  using var input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
  if(input.Length<offset || born!=creation){offset=0;pending="";creation=born;}
  // Bound work per poll, including on a huge existing game log.
  if(input.Length-offset>MaxBytes){offset=input.Length-MaxBytes;pending="";}
  input.Position=offset;
  byte[] bytes=new byte[(int)Math.Min(64*1024,input.Length-offset)];
  int count=input.Read(bytes,0,bytes.Length);offset+=count;
  pending+=Encoding.UTF8.GetString(bytes,0,count);
  int end=pending.LastIndexOf('\n');if(end<0){if(pending.Length>8192)pending="";return 0;}
  var complete=pending[..(end+1)];pending=pending[(end+1)..];int copied=0;
  foreach(var line in complete.Split('\n'))
  {
   var match=Event.Match(line);
   if(match.Success){Append(match.Value);copied++;}
   else if(line.Contains("Fatal error:",StringComparison.OrdinalIgnoreCase)||line.Contains("Unhandled Exception:",StringComparison.OrdinalIgnoreCase)||line.Contains("Assertion failed:",StringComparison.OrdinalIgnoreCase))
   {Append("ICARUS reported a fatal error. Keep the original game log and crash folder; this does not identify the cause.");copied++;}
  }
  return copied;
 }
 void Append(string message)
 {
  Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
  if(File.Exists(destination)&&new FileInfo(destination).Length>=MaxBytes)File.Move(destination,destination+".previous",true);
  File.AppendAllText(destination,$"{DateTime.UtcNow:O} {message}\n",new UTF8Encoding(false));
 }
 public static string DefaultSource => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Icarus","Saved","Logs","Icarus.log");
 public static string DefaultFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VariantModOrganizer","DovaOutPut");
 public static async Task WatchGame(string source,string folder,string role)
 {
  if(role is not ("Client" or "Server"))throw new ArgumentException("Choose Client or Server.");
  Directory.CreateDirectory(folder);
  // One background reader for each output. This also prevents duplicate workers
  // if the launch button is used twice while Steam is still opening the game.
  using var guard=new FileStream(Path.Combine(folder,$"{role}.collector.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
  var reader=new DovaOutputLog(source,folder,role);var started=DateTime.UtcNow;DateTime? lastSeen=null;
  while(true)
  {
   bool running=false;
   foreach(var p in System.Diagnostics.Process.GetProcesses())using(p)
    try{if(p.ProcessName.StartsWith(role=="Server"?"IcarusServer":"Icarus",StringComparison.OrdinalIgnoreCase)&& (role=="Server"||!p.ProcessName.StartsWith("IcarusServer",StringComparison.OrdinalIgnoreCase)))running=true;}catch{}
   if(running)lastSeen=DateTime.UtcNow;
   try{reader.Poll();}catch(IOException){}catch(UnauthorizedAccessException){return;}
   if(!running && DateTime.UtcNow-(lastSeen??started)>TimeSpan.FromSeconds(lastSeen.HasValue?10:120))return;
   await Task.Delay(2000);
  }
 }
}
