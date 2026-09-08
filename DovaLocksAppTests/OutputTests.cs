using DovaInstaller;
static class OutputTests
{
 public static void Run(string root)
 {
  string folder=Path.Combine(root,"diagnostics"),source=Path.Combine(root,"Icarus.log");
  File.WriteAllText(source,"private unrelated account data\nLogTemp: DovaOutPut 1.0.7-test [client] client opening menu code=0\n");
  var reader=new DovaOutputLog(source,folder,"Client");
  if(reader.Poll()!=1||reader.Poll()!=0)throw new Exception("Log duplicated entries");
  File.AppendAllText(source,"DovaOutPut 1.0.7-test [client] client action result code=");reader.Poll();File.AppendAllText(source,"5\nFatal error: private user path\n");
  if(reader.Poll()!=2)throw new Exception("Split line or crash marker lost");
  string text=File.ReadAllText(reader.Destination);
  if(text.Contains("private")||!text.Contains("code=5")||!text.Contains("does not identify the cause"))throw new Exception("Diagnostic privacy or attribution failed");
  File.WriteAllText(source,"DovaOutPut 1.0.7-test [server] base link requested code=0\n");
  if(reader.Poll()!=1)throw new Exception("Truncated source not reopened");
  File.WriteAllText(reader.Destination,new string('x',DovaOutputLog.MaxBytes));File.AppendAllText(source,"DovaOutPut 1.0.7-test [server] base link requested code=1\n");reader.Poll();
  if(!File.Exists(reader.Destination+".previous")||new FileInfo(reader.Destination).Length>1024)throw new Exception("Rotation failed");
  Console.WriteLine("PASS diagnostics: shared read, incremental and split lines, source reset, privacy, crash attribution, 1 MB rotation");
 }
}
