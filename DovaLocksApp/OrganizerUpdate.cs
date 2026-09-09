using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace DovaInstaller;

public record OrganizerRelease(Version Version,string Url,string Hash,long Size,string Notes);
public sealed class OrganizerUpdate
{
 public const string Repository="VariantCreator/Variant-Mod-Organizer";
 public const string AssetName="Variant-Mod-Organizer-Installer.exe";
 public const string Endpoint="https://api.github.com/repos/"+Repository+"/releases/latest";
 public static Version CurrentVersion=>Assembly.GetExecutingAssembly().GetName().Version??new Version(1,5,0);
 readonly HttpClient http;
 public OrganizerUpdate(HttpClient? client=null){http=client??new HttpClient{Timeout=TimeSpan.FromMinutes(5)};http.DefaultRequestHeaders.UserAgent.ParseAdd("Variant-Mod-Organizer/1.5.0");}
 public static OrganizerRelease Parse(string json)
 {
  using var doc=JsonDocument.Parse(json);var r=doc.RootElement;
  if(r.GetProperty("draft").GetBoolean()||r.GetProperty("prerelease").GetBoolean())throw new IOException("This organizer update isn't ready yet.");
  string tag=r.GetProperty("tag_name").GetString()??"";
  if(!Regex.IsMatch(tag,@"^v?\d+\.\d+\.\d+$")||!Version.TryParse(tag.TrimStart('v'),out var version))throw new IOException("Couldn't read the organizer version.");
  var files=r.GetProperty("assets").EnumerateArray().Where(a=>a.GetProperty("name").GetString()==AssetName).ToArray();
  if(files.Length!=1)throw new IOException("This release needs one organizer installer.");
  var file=files[0];string url=file.GetProperty("browser_download_url").GetString()??"";
  if(url!=$"https://github.com/{Repository}/releases/download/{tag}/{AssetName}")throw new IOException("The update isn't from Dova's organizer repository.");
  string digest=file.GetProperty("digest").GetString()??"";long size=file.GetProperty("size").GetInt64();
  if(!Regex.IsMatch(digest,@"^sha256:[a-fA-F0-9]{64}$")||size<1||size>256L*1024*1024)throw new IOException("This update is missing valid download verification.");
  string notes=r.TryGetProperty("body",out var body)?body.GetString()??"":"";
  return new(version,url,digest[7..].ToLowerInvariant(),size,notes.Length>12000?notes[..12000]:notes);
 }
 public async Task<OrganizerRelease> Latest(CancellationToken token=default)=>Parse(await http.GetStringAsync(Endpoint,token));
 public static bool IsNewer(Version remote,Version current)=>new Version(remote.Major,remote.Minor,Math.Max(0,remote.Build))>new Version(current.Major,current.Minor,Math.Max(0,current.Build));
 public async Task<string> Download(OrganizerRelease release,string folder,IProgress<double>? progress=null,CancellationToken token=default)
 {
  Directory.CreateDirectory(folder);string staging=Path.Combine(folder,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(staging);string path=Path.Combine(staging,AssetName);
  try {
   using var response=await http.GetAsync(release.Url,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();
   if(response.Content.Headers.ContentLength is long expected&&expected!=release.Size)throw new IOException("The update download changed. Check again.");
   using var input=await response.Content.ReadAsStreamAsync(token);
   using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true)){
    byte[] buffer=new byte[81920];long total=0;int count;
    while((count=await input.ReadAsync(buffer,token))>0){total+=count;if(total>release.Size)throw new IOException("The update was larger than expected.");await output.WriteAsync(buffer.AsMemory(0,count),token);progress?.Report(100d*total/release.Size);}
    if(total!=release.Size)throw new IOException("The update download was incomplete.");
   }
   using(var read=File.OpenRead(path))if(!Convert.ToHexString(await SHA256.HashDataAsync(read,token)).Equals(release.Hash,StringComparison.OrdinalIgnoreCase))throw new IOException("The update couldn't be verified. Your installed app hasn't changed.");
   var fresh=await Latest(token);if(fresh.Version!=release.Version||fresh.Hash!=release.Hash||fresh.Url!=release.Url||fresh.Size!=release.Size)throw new IOException("A new release appeared while downloading. Check again.");
   return path;
  }catch{if(File.Exists(path))File.Delete(path);if(Directory.Exists(staging))Directory.Delete(staging);throw;}
 }
 public static ProcessStartInfo InstallerCommand(string installer,string installFolder)
 {
  var start=new ProcessStartInfo(Path.GetFullPath(installer)){UseShellExecute=false};
  foreach(string arg in new[]{"/SILENT","/SUPPRESSMSGBOXES","/NORESTART","/CLOSEAPPLICATIONS","/ORGANIZERUPDATE=1","/DIR="+Path.GetFullPath(installFolder)})start.ArgumentList.Add(arg);
  return start;
 }
}
