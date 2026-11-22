using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace AG4O3A {
 public sealed class UpdateRelease {
  public string tag_name { get; set; }
  public bool draft { get; set; }
  public bool prerelease { get; set; }
  public UpdateAsset[] assets { get; set; }
  public Version Version { get; set; }
  public UpdateAsset Installer { get; set; }
  public UpdateAsset Checksums { get; set; }
 }
 public sealed class UpdateAsset {
  public string name { get; set; }
  public string browser_download_url { get; set; }
  public long size { get; set; }
 }
 public static class Updates {
  const string Repository = "https://github.com/CT7ASY/Antena-Genius-4O3A/";
  sealed class Client : WebClient {
   public Client() { Headers[HttpRequestHeader.UserAgent] = "AG4O3A/"+Program.Version; }
   protected override WebRequest GetWebRequest(Uri address) {
    var request = base.GetWebRequest(address);
    request.Timeout = 60000;
    var http = request as HttpWebRequest;
    if(http != null) http.ReadWriteTimeout = 60000;
    return request;
   }
  }
  public static UpdateRelease Parse(string json, Version current) {
   var release = new JavaScriptSerializer().Deserialize<UpdateRelease>(json);
   Version version;
   if(release == null || release.draft || release.prerelease ||
      !Regex.IsMatch(release.tag_name ?? "", @"^v?\d+\.\d+\.\d+$") ||
      !Version.TryParse(release.tag_name.TrimStart('v'),out version) || version <= current) return null;
   release.Version = version;
   var assets = release.assets ?? new UpdateAsset[0];
   release.Installer = assets.FirstOrDefault(a=>a.name=="Instalar-AG-4O3A-"+version+".exe");
   release.Checksums = assets.FirstOrDefault(a=>a.name=="SHA256SUMS.txt");
   if(release.Installer==null || release.Checksums==null ||
      release.Installer.size<1 || release.Installer.size>100*1024*1024 ||
      release.Checksums.size<1 || release.Checksums.size>65536)
    throw new InvalidDataException("A atualização está incompleta.");
   foreach(var asset in new[]{release.Installer,release.Checksums}) {
    string expected=Repository+"releases/download/"+release.tag_name+"/"+asset.name;
    if(asset.browser_download_url!=expected) throw new InvalidDataException("Endereço de atualização inválido.");
   }
   return release;
  }
  public static UpdateRelease Check() {
   ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
   using(var client=new Client()) {
    return Parse(client.DownloadString("https://api.github.com/repos/CT7ASY/Antena-Genius-4O3A/releases/latest"),
      new Version(Program.Version));
   }
  }
  public static void Verify(string path,string sums,string name) {
   string expected=null;
   foreach(var line in sums.Split('\n')) {
    var match=Regex.Match(line.Trim(),@"^([0-9a-fA-F]{64})\s+\*?(.+)$");
    if(match.Success && match.Groups[2].Value==name) {
     if(expected!=null) throw new InvalidDataException("Checksum duplicado.");
     expected=match.Groups[1].Value;
    }
   }
   if(expected==null) throw new InvalidDataException("Checksum da atualização em falta.");
   using(var stream=File.OpenRead(path))
   using(var sha=SHA256.Create()) {
    string actual=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");
    if(!string.Equals(actual,expected,StringComparison.OrdinalIgnoreCase))
     throw new InvalidDataException("A verificação do ficheiro de atualização falhou.");
   }
  }
  public static string Download(UpdateRelease release) {
   string folder=Path.Combine(Storage.Root,"updates",Guid.NewGuid().ToString("N"));
   Directory.CreateDirectory(folder);
   string path=Path.Combine(folder,release.Installer.name);
   try {
    using(var client=new Client()) {
     string sums=client.DownloadString(release.Checksums.browser_download_url);
     client.DownloadFile(release.Installer.browser_download_url,path);
     if(new FileInfo(path).Length!=release.Installer.size) throw new InvalidDataException("Transferência incompleta.");
     Verify(path,sums,release.Installer.name);
    }
    return path;
   } catch { if(File.Exists(path))File.Delete(path);throw; }
  }
 }
}

