using System.IO;
namespace DovaInstaller;
public static class DiagnosticPaths
{
 public static string StartFolder(bool crash, string? localData = null)
 {
  string saved=Path.Combine(localData??Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Icarus","Saved");
  string preferred=Path.Combine(saved,crash?"Crashes":"Logs");
  if(Directory.Exists(preferred))return preferred;
  if(Directory.Exists(saved))return saved;
  return localData??Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
 }
}
