using DovaInstaller;
using System.Text;
static class CrashReportTests
{
 public static void Run(string root)
 {
  string dir=Path.Combine(root,"crash-report");Directory.CreateDirectory(dir);string xml=Path.Combine(dir,"CrashContext.runtime-xml");
  File.WriteAllText(xml,"<FGenericCrashContext><RuntimeProperties><ErrorMessage>Unhandled Exception: EXCEPTION_ACCESS_VIOLATION</ErrorMessage><GameName>UE4-Icarus</GameName><CallStack>module!function</CallStack><LoginId>PRIVATE-ID</LoginId></RuntimeProperties></FGenericCrashContext>");
  var original=File.ReadAllBytes(xml);string text=UnrealCrashReport.Read(xml).Details;
  if(!text.Contains("EXCEPTION_ACCESS_VIOLATION")||!text.Contains("module!function")||text.Contains("PRIVATE-ID"))throw new Exception("Crash context fields not extracted correctly");
  var dump=new byte[84];BitConverter.GetBytes(0x504d444du).CopyTo(dump,0);BitConverter.GetBytes(1u).CopyTo(dump,8);BitConverter.GetBytes(32u).CopyTo(dump,12);BitConverter.GetBytes(6u).CopyTo(dump,32);BitConverter.GetBytes(40u).CopyTo(dump,36);BitConverter.GetBytes(44u).CopyTo(dump,40);BitConverter.GetBytes(42u).CopyTo(dump,44);BitConverter.GetBytes(0xc0000005u).CopyTo(dump,52);BitConverter.GetBytes(0x12345678ul).CopyTo(dump,68);
  string dmp=Path.Combine(dir,"UE4Minidump.dmp");File.WriteAllBytes(dmp,dump);text=UnrealCrashReport.Read(dmp).Details;
  if(!text.Contains("0xC0000005")||!text.Contains("0000000012345678")||!text.Contains("module!function"))throw new Exception("Dump record or companion context lost");
  File.Move(xml,xml+".kept");if(!UnrealCrashReport.Read(dmp).Details.Contains("matching game symbols"))throw new Exception("Standalone dump limitation not explained");File.Move(xml+".kept",xml);
  string log=Path.Combine(dir,"Icarus.log");File.WriteAllText(log,"normal log\nFatal error: allocation failed\nmodule!frame\n",Encoding.Unicode);text=UnrealCrashReport.Read(log).Details;if(!text.Contains("allocation failed")||!text.Contains("module!frame"))throw new Exception("UTF16 log not decoded");
  File.WriteAllText(log,"LogInit: regular session\nLogTemp: last entry");if(!UnrealCrashReport.Read(log).Details.Contains("No explicit crash marker"))throw new Exception("Normal log called a crash");
  void Reject(string path){try{UnrealCrashReport.Read(path);}catch(Exception ex)when(ex is System.Xml.XmlException or InvalidDataException){return;}throw new Exception("Malformed report accepted");}
  File.WriteAllText(Path.Combine(dir,"bad.xml"),"<!DOCTYPE x [<!ENTITY secret SYSTEM 'file:///missing'>]><FGenericCrashContext>&secret;</FGenericCrashContext>");Reject(Path.Combine(dir,"bad.xml"));
  File.WriteAllBytes(Path.Combine(dir,"bad.dmp"),dump[..50]);Reject(Path.Combine(dir,"bad.dmp"));
  if(!File.ReadAllBytes(xml).SequenceEqual(original))throw new Exception("Crash source modified");
  string readme=Path.Combine(dir,"Read me.txt");File.WriteAllText(readme,"Dova Locks installation instructions");Reject(readme);
  string fakeLog=Path.Combine(dir,"Read me.log");File.WriteAllText(fakeLog,"Dova Locks installation instructions");Reject(fakeLog);
  string local=Path.Combine(root,"another-user");Directory.CreateDirectory(Path.Combine(local,"Icarus","Saved","Crashes"));Directory.CreateDirectory(Path.Combine(local,"Icarus","Saved","Logs"));
  if(DiagnosticPaths.StartFolder(true,local)!=Path.Combine(local,"Icarus","Saved","Crashes")||DiagnosticPaths.StartFolder(false,local)!=Path.Combine(local,"Icarus","Saved","Logs"))throw new Exception("Picker did not use this user's ICARUS folders");
  Console.WriteLine("PASS crash viewer: context, dump exception and companion context, standalone dump, UTF16 log, normal log, malformed/DTD rejection, originals unchanged");
 }
}
