using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace DovaInstaller;
public record ModRelease(string Version, string Url, string Hash, long Size, string Notes);
public sealed class ReleaseClient
{
 public const string Endpoint = "https://api.github.com/repos/VariantCreator/Dova-Locks/releases/latest";
 public const int MaxBytes = 64 * 1024 * 1024;
 readonly HttpClient http;
 public ReleaseClient(HttpClient? client = null) { http = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) }; http.DefaultRequestHeaders.UserAgent.ParseAdd("Variant-Mod-Organizer/1.3.0"); }
 public static ModRelease Parse(string json)
 {
  using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
  if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean()) throw new IOException("This release is not ready for players.");
  var tag = root.GetProperty("tag_name").GetString() ?? "";
  if (!Regex.IsMatch(tag, @"^v?\d+\.\d+\.\d+$")) throw new IOException("The release version could not be read.");
  var files = root.GetProperty("assets").EnumerateArray().Where(a => a.GetProperty("name").GetString() == InstallCore.FileName).ToArray();
  if (files.Length != 1) throw new IOException("The latest release does not have a ready mod download.");
  var file = files[0]; string url = file.GetProperty("browser_download_url").GetString() ?? "";
  if (url != $"https://github.com/VariantCreator/Dova-Locks/releases/download/{tag}/{InstallCore.FileName}") throw new IOException("The download is not from Dova's release page.");
  var digest = file.GetProperty("digest").GetString() ?? "";
  if (!Regex.IsMatch(digest, "^sha256:[a-fA-F0-9]{64}$")) throw new IOException("The release is missing its download verification.");
  long size = file.GetProperty("size").GetInt64(); if (size <= 0 || size > MaxBytes) throw new IOException("The download size is not supported.");
  string notes = root.TryGetProperty("body",out var body) ? body.GetString() ?? "" : "";
  return new(tag.TrimStart('v'),url,digest[7..].ToLowerInvariant(),size,notes.Length > 12000 ? notes[..12000] : notes);
 }
 public async Task<ModRelease> Latest() => Parse(await http.GetStringAsync(Endpoint));
 public async Task<(ModRelease Release,byte[] Bytes)> CurrentDownload()
 {
  var release=await Latest();var bytes=await Download(release);var current=await Latest();
  if(current.Version!=release.Version||current.Hash!=release.Hash||current.Size!=release.Size||current.Url!=release.Url)throw new IOException("A new release appeared while downloading. Click Install / Update again to get it. Nothing was installed.");
  return(release,bytes);
 }
 public async Task<byte[]> Download(ModRelease release)
 {
  using var response = await http.GetAsync(release.Url, HttpCompletionOption.ResponseHeadersRead); response.EnsureSuccessStatusCode();
  if (response.Content.Headers.ContentLength is long length && length != release.Size) throw new IOException("The download size changed. Check for updates again.");
  using var input = await response.Content.ReadAsStreamAsync(); using var output = new MemoryStream();
  var buffer = new byte[81920]; using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
  int read; while((read=await input.ReadAsync(buffer,timeout.Token))>0) { if(output.Length + read > release.Size) throw new IOException("The download was larger than expected."); output.Write(buffer,0,read); }
  var bytes = output.ToArray(); if(bytes.LongLength != release.Size || !Convert.ToHexString(SHA256.HashData(bytes)).Equals(release.Hash,StringComparison.OrdinalIgnoreCase)) throw new IOException("The download could not be verified. Nothing was installed; please try again.");
  return bytes;
 }
}
public static class AppPreferences
{
 static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DovaLocks","settings.json");
 public static string? Load() { try { return JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(FilePath))?.GetValueOrDefault("gameFolder"); } catch { return null; } }
 public static void Save(string root) { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!); var temp = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp"; try { File.WriteAllText(temp,JsonSerializer.Serialize(new {gameFolder=root})); File.Move(temp,FilePath,true); } finally { if(File.Exists(temp))File.Delete(temp); } }
}
