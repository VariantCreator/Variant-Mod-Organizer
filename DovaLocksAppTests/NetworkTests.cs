using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using DovaInstaller;
public static class NetworkTests
{
 public static async Task Run()
 {
  byte[] bytes="verified new pak"u8.ToArray();string hash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
  string Json(string url="https://github.com/VariantCreator/Dova-Locks/releases/download/v9.8.7/Dova-Locks_P.pak",bool draft=false,bool prerelease=false,string? digest=null,long? size=null)=>JsonSerializer.Serialize(new {tag_name="v9.8.7",draft,prerelease,body="Player notes",assets=new[]{new{name=InstallCore.FileName,browser_download_url=url,digest=digest??"sha256:"+hash,size=size??bytes.Length}}});
  var release=ReleaseClient.Parse(Json());if(release.Version!="9.8.7")throw new Exception("version parse");
  foreach(var json in new[]{Json(url:"https://example.com/mod.pak"),Json(draft:true),Json(prerelease:true),Json(digest:""),Json(size:ReleaseClient.MaxBytes+1L)}){try{ReleaseClient.Parse(json);throw new Exception("Invalid release accepted");}catch(IOException){}}
  Console.WriteLine("PASS: private drafts, prereleases, external files, missing verification and oversized releases rejected");
  var client=new ReleaseClient(new HttpClient(new Fake(bytes)));var got=await client.Download(release);if(!got.SequenceEqual(bytes))throw new Exception("download mismatch");
  foreach(var bad in new[]{"broken"u8.ToArray(),new byte[bytes.Length],new byte[bytes.Length+1]}){try{await new ReleaseClient(new HttpClient(new Fake(bad))).Download(release);throw new Exception("corrupt download accepted");}catch(IOException){}}
  Console.WriteLine("PASS: valid download accepted; truncated, oversized and corrupt downloads rejected");
  var stable=await new ReleaseClient(new HttpClient(new Sequence(Json(),Json(),bytes))).CurrentDownload();if(stable.Release.Version!="9.8.7")throw new Exception("fresh version not used");
  try{await new ReleaseClient(new HttpClient(new Sequence(Json(),Json().Replace("9.8.7","9.8.8"),bytes))).CurrentDownload();throw new Exception("changed release accepted");}catch(IOException){}
  Console.WriteLine("PASS: install checks release before and after download; publication during download stops installation");
  var live=new ReleaseClient();var current=await live.CurrentDownload();var latest=current.Release;var downloaded=current.Bytes;if(downloaded.Length!=latest.Size)throw new Exception("live mismatch");
  var fixture=Path.Combine(Path.GetTempPath(),"Variant-Live-Install-"+Guid.NewGuid().ToString("N"));foreach(var dir in new[]{"Content/Data","Content/Paks","Binaries/Win64"})Directory.CreateDirectory(Path.Combine(fixture,dir));File.WriteAllText(Path.Combine(fixture,"Content/Data/data.pak"),"fixture");File.WriteAllText(Path.Combine(fixture,"Binaries/Win64/Icarus-Win64-Shipping.exe"),"fixture");
  var installed=InstallCore.Apply(fixture,()=>new MemoryStream(downloaded),latest.Hash,_=>{},release:latest.Version);
  if(installed.Destination!=Path.Combine(fixture,"Content","Paks","mods",InstallCore.FileName)||InstallCore.Hash(installed.Destination)!=latest.Hash)throw new Exception("live install location/content mismatch");
  Console.WriteLine("PASS: live public release "+latest.Version+" downloaded and installed into the selected isolated fixture; installed file verified; no real game files changed");
 }
 sealed class Fake(byte[] bytes):HttpMessageHandler {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)});}
 sealed class Sequence(string first,string second,byte[] bytes):HttpMessageHandler
 {
  int reads;
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=request.RequestUri!.Host=="api.github.com"?new StringContent(reads++==0?first:second):new ByteArrayContent(bytes)});
 }
}
