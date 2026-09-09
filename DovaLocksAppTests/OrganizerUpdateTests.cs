using DovaInstaller;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
static class OrganizerUpdateTests
{
 static string Metadata(string hash,string tag="v1.6.0",string? url=null,bool draft=false,bool prerelease=false)=>JsonSerializer.Serialize(new{tag_name=tag,draft,prerelease,body="A nicer organizer.",assets=new[]{new{name=OrganizerUpdate.AssetName,browser_download_url=url??$"https://github.com/{OrganizerUpdate.Repository}/releases/download/{tag}/{OrganizerUpdate.AssetName}",digest="sha256:"+hash,size=3}}});
 public static async Task Run(string root)
 {
  byte[] bytes=[1,2,3];string hash=Convert.ToHexString(SHA256.HashData(bytes));string json=Metadata(hash);var release=OrganizerUpdate.Parse(json);
  if(!OrganizerUpdate.IsNewer(release.Version,new Version(1,5,0,0))||OrganizerUpdate.IsNewer(new Version(1,5,0),new Version(1,5,0,0))||OrganizerUpdate.IsNewer(new Version(1,4,0),new Version(1,5,0)))throw new Exception("Organizer version comparison failed");
  foreach(string bad in new[]{Metadata(hash,url:"https://example.com/update.exe"),Metadata(hash,draft:true),Metadata(hash,prerelease:true),Metadata("bad"),Metadata(hash,tag:"v1.6.0-test")}){try{OrganizerUpdate.Parse(bad);throw new Exception("Untrusted release accepted");}catch(IOException){}}
  var client=new OrganizerUpdate(new HttpClient(new Handler(bytes,json)));string folder=Path.Combine(root,"organizer-update");string downloaded=await client.Download(release,folder);if(!File.ReadAllBytes(downloaded).SequenceEqual(bytes))throw new Exception("Update payload changed");
  var command=OrganizerUpdate.InstallerCommand(downloaded,Path.Combine(root,"custom install"));if(!command.ArgumentList.Contains("/DIR="+Path.GetFullPath(Path.Combine(root,"custom install")))||command.UseShellExecute)throw new Exception("Update changed install directory or uses shell text");
  foreach(var handler in new[]{new Handler([1,2,4],json),new Handler(bytes,Metadata(new string('a',64)))}){
   var failing=new OrganizerUpdate(new HttpClient(handler));string failureFolder=Path.Combine(root,"bad-update-"+Guid.NewGuid());try{await failing.Download(release,failureFolder);throw new Exception("Bad or changed update accepted");}catch(IOException){}
   if(Directory.EnumerateFiles(failureFolder,"*",SearchOption.AllDirectories).Any())throw new Exception("Rejected installer left on disk");
  }
  using var cancelled=new CancellationTokenSource();cancelled.Cancel();try{await client.Download(release,Path.Combine(root,"cancelled"),token:cancelled.Token);throw new Exception("Cancelled update continued");}catch(OperationCanceledException){}
  Console.WriteLine("PASS organizer updates: trusted repository, stable release, correct hash/size, changed-release rejection, cancellation, version comparison, preserved installation path");
 }
 sealed class Handler(byte[] bytes,string json):HttpMessageHandler
 {
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){token.ThrowIfCancellationRequested();return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=request.RequestUri!.AbsoluteUri==OrganizerUpdate.Endpoint?new StringContent(json):new ByteArrayContent(bytes)});}
 }
}
