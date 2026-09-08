using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DovaInstaller;
public sealed record UnrealCrashReport(string FilePath,string Details)
{
 public static UnrealCrashReport Read(string path)
 {
  path=Path.GetFullPath(path);string ext=Path.GetExtension(path).ToLowerInvariant();
  string details=ext switch {".runtime-xml" or ".xml"=>ReadContext(path),".dmp"=>ReadDump(path),".log" or ".txt"=>ReadLog(path),_=>throw new InvalidDataException("Choose CrashContext.runtime-xml, a .dmp, or a .log file.")};
  return new(path,details+"\n\nThis is what Unreal reported. It does not prove that Dova Locks, another mod, or your hardware caused the crash. Keep the original crash folder.");
 }
 static FileStream Open(string path)=>new(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
 static string ReadContext(string path)
 {
  using var file=Open(path);if(file.Length>16*1024*1024)throw new InvalidDataException("This crash context is unusually large. Choose the original CrashContext.runtime-xml file.");
  using var reader=XmlReader.Create(file,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=4*1024*1024});
  XDocument doc=XDocument.Load(reader);if(doc.Root?.Name.LocalName!="FGenericCrashContext")throw new InvalidDataException("This XML is not an Unreal crash context.");
  var sb=new StringBuilder("UNREAL CRASH REPORT\n");bool found=false;
  foreach(var pair in new[]{("ErrorMessage","Reported error"),("CrashType","Crash type"),("GameName","Game"),("BuildVersion","Build"),("EngineVersion","Engine"),("Misc.OSVersionMajor","Windows"),("Misc.CPUBrand","Processor"),("Misc.PrimaryGPUBrand","Graphics card"),("MemoryStats.bIsOOM","Out of memory flag"),("CallStack","Call stack"),("PCallStack","Portable call stack")})
  {
   var value=doc.Descendants().FirstOrDefault(x=>x.Name.LocalName==pair.Item1)?.Value.Trim();
   if(string.IsNullOrEmpty(value))continue;found=true;sb.Append("\n").Append(pair.Item2).Append(":\n").Append(value.Length>8000?value[..8000]+"\n[truncated]":value).Append('\n');
  }
  if(!found)sb.Append("\nThe file was read, but it does not contain an error or call stack. Try the .log from the same crash folder.");
  return sb.ToString();
 }
 static string ReadDump(string path)
 {
  var sb=new StringBuilder("UNREAL MINIDUMP\n");
  using(var file=Open(path))using(var r=new BinaryReader(file))
  {
   if(file.Length<32||r.ReadUInt32()!=0x504d444d)throw new InvalidDataException("This is not a valid Windows minidump.");
   r.ReadUInt32();uint count=r.ReadUInt32(),directory=r.ReadUInt32();
   if(count>4096||directory>file.Length-(long)count*12)throw new InvalidDataException("The minidump directory is incomplete.");
   bool exception=false;
   for(uint i=0;i<count;i++)
   {
    file.Position=directory+i*12;uint type=r.ReadUInt32(),size=r.ReadUInt32(),offset=r.ReadUInt32();
    if(offset>file.Length-(long)size)throw new InvalidDataException("The minidump is incomplete.");
    if(type!=6)continue;if(size<40)throw new InvalidDataException("The exception record is incomplete.");
    file.Position=offset;uint thread=r.ReadUInt32();r.ReadUInt32();uint code=r.ReadUInt32();r.ReadUInt32();r.ReadUInt64();ulong address=r.ReadUInt64();
    sb.Append($"\nException code: 0x{code:X8}\nException address: 0x{address:X16}\nThread: {thread}\n");exception=true;break;
   }
   if(!exception)sb.Append("\nThe dump has no exception record.");
  }
  string? context=Directory.EnumerateFiles(Path.GetDirectoryName(path)!).Take(256).FirstOrDefault(p=>Path.GetFileName(p).Equals("CrashContext.runtime-xml",StringComparison.OrdinalIgnoreCase));
  if(context!=null){try{sb.Append("\n\nCompanion CrashContext.runtime-xml:\n").Append(ReadContext(context));}catch(Exception ex) when(ex is IOException or XmlException or InvalidDataException){sb.Append("\nThe companion context could not be read: ").Append(ex.Message);}}
  else sb.Append("\nPut CrashContext.runtime-xml from the same crash folder beside this dump for the error text. A full dump analysis needs a debugger and matching game symbols; this viewer does not do that analysis.");
  return sb.ToString();
 }
 static string ReadLog(string path)
 {
  const int limit=2*1024*1024;using var file=Open(path);long length=file.Length;if(length==0)return "The log is empty.";
  int a=file.ReadByte(),b=file.ReadByte();Encoding encoding=a==255&&b==254?Encoding.Unicode:a==254&&b==255?Encoding.BigEndianUnicode:Encoding.UTF8;
  long start=Math.Max(0,length-limit);if(encoding!=Encoding.UTF8)start-=start%2;file.Position=start;
  byte[] buffer=new byte[(int)(length-start)];int read=0;while(read<buffer.Length){int n=file.Read(buffer,read,buffer.Length-read);if(n==0)break;read+=n;}
  string text=encoding.GetString(buffer,0,read);if(text.Contains('\0'))throw new InvalidDataException("This looks like a binary file. Choose the .dmp or CrashContext.runtime-xml instead.");
  var lines=text.Split('\n');int index=Array.FindLastIndex(lines,s=>s.Contains("Fatal error",StringComparison.OrdinalIgnoreCase)||s.Contains("Unhandled Exception",StringComparison.OrdinalIgnoreCase)||s.Contains("Assertion failed",StringComparison.OrdinalIgnoreCase));
  string heading=index>=0?"CRASH DETAILS FROM LOG":"No explicit crash marker found. Here are the latest log lines.";
  string excerpt=string.Join('\n',lines.Skip(index>=0?Math.Max(0,index-5):Math.Max(0,lines.Length-60)).Take(160));
  if(excerpt.Length>24000)excerpt=excerpt[..24000]+"\n[truncated]";
  return heading+"\n\n"+excerpt;
 }
}
