using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
namespace AG4O3A {
public class Fields : Dictionary<string,string> {
 public string Get(string key){string v;return TryGetValue(key,out v)?v:"";}
 public int Int(string key){int v;return int.TryParse(Get(key),out v)?v:0;}
 public Fields Copy(){var f=new Fields();foreach(var p in this)f[p.Key]=p.Value;return f;}
 public static Fields Parse(string s){var f=new Fields();foreach(Match m in Regex.Matches(s,@"(\w+)=([^\s]*)"))f[m.Groups[1].Value]=m.Groups[2].Value;return f;}
}
public static class Storage {
 public static string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CT7ASY","AG4O3A");
 public static string PathFor(string name){Directory.CreateDirectory(Root);return Path.Combine(Root,name);}
 public static void Log(string s){try{File.AppendAllText(PathFor("diagnostico.log"),DateTime.UtcNow.ToString("o")+" "+s+Environment.NewLine);}catch{}}
}
public class Profile {
 public string Mode="Local",LocalHost="127.0.0.1",RemoteHost="example.invalid",PasswordProtected="";
 public int LocalPort=9007,RemotePort=9007;
 public bool Remote {get{return Mode=="Remota";}}
 public string Address {get{return Remote?RemoteHost:LocalHost;}}
 public int Port {get{return Remote?RemotePort:LocalPort;}}
 public Profile Copy(){return (Profile)MemberwiseClone();}
 public static Profile Load(){
  string path=Storage.PathFor("connection.json");
  if(!File.Exists(path)){
   string[] legacy={Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"connection.json"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Antenna-View","connection.json")};
   foreach(string source in legacy)if(File.Exists(source)){var p=new JavaScriptSerializer().Deserialize<Profile>(File.ReadAllText(source));p.Validate();p.Save();Storage.Log("Perfil anterior importado");return p;}
   return new Profile();
  }
  var profile=new JavaScriptSerializer().Deserialize<Profile>(File.ReadAllText(path));profile.Validate();return profile;
 }
 public void Validate(){if(Mode!="Local"&&Mode!="Remota")throw new Exception("Tipo de ligação inválido.");ValidateHost(LocalHost);ValidateHost(RemoteHost);if(LocalPort<1||LocalPort>65535||RemotePort<1||RemotePort>65535)throw new Exception("Porta TCP inválida.");}
 public static void ValidateHost(string v){if(string.IsNullOrWhiteSpace(v)||Regex.IsMatch(v,@"[\s/\\:]")||Uri.CheckHostName(v)==UriHostNameType.Unknown)throw new Exception("Introduza um hostname ou IPv4 válido, sem protocolo nem porta.");}
 public string Password(){
  if(string.IsNullOrEmpty(PasswordProtected))return "";
  try{
   byte[] bytes;
   if(PasswordProtected.StartsWith("dpapi:"))bytes=Convert.FromBase64String(PasswordProtected.Substring(6));
   else {if(PasswordProtected.Length%2!=0)throw new Exception();bytes=Enumerable.Range(0,PasswordProtected.Length/2).Select(i=>Convert.ToByte(PasswordProtected.Substring(i*2,2),16)).ToArray();}
   byte[] plain=ProtectedData.Unprotect(bytes,null,DataProtectionScope.CurrentUser);
   try{return Encoding.Unicode.GetString(plain);}finally{Array.Clear(plain,0,plain.Length);}
  }catch{throw new Exception("Volte a introduzir a palavra-passe remota neste computador.");}
 }
 public void SetPassword(string value){if(value.Length>0&&!Regex.IsMatch(value,@"^[\x21-\x7E]+$"))throw new Exception("Palavra-passe remota inválida.");if(value.Length==0){PasswordProtected="";return;}byte[] b=Encoding.Unicode.GetBytes(value);try{PasswordProtected="dpapi:"+Convert.ToBase64String(ProtectedData.Protect(b,null,DataProtectionScope.CurrentUser));}finally{Array.Clear(b,0,b.Length);}}
 public void Save(){Validate();string p=Storage.PathFor("connection.json"),tmp=p+".tmp";File.WriteAllText(tmp,new JavaScriptSerializer().Serialize(this),Encoding.UTF8);if(File.Exists(p))File.Replace(tmp,p,null);else File.Move(tmp,p);}
}
public class Wire : IDisposable {
 public TcpClient Client;string buffer="";public DateTime Last=DateTime.UtcNow;
 public void Open(string host,int port){Dispose();Client=new TcpClient();var t=Client.ConnectAsync(host,port);if(!t.Wait(5000)){Dispose();throw new Exception("Tempo de ligação excedido.");}t.GetAwaiter().GetResult();Client.GetStream().WriteTimeout=5000;buffer="";Last=DateTime.UtcNow;}
 public void Send(string line){byte[] b=Encoding.ASCII.GetBytes(line+"\r\n");Client.GetStream().Write(b,0,b.Length);}
 public string Read(int wait){DateTime until=DateTime.UtcNow.AddMilliseconds(wait);while(true){int end=buffer.IndexOf('\n');if(end>=0){string line=buffer.Substring(0,end).TrimEnd('\r');buffer=buffer.Substring(end+1);return line;}if(Client==null)throw new Exception("Sem ligação.");var stream=Client.GetStream();if(stream.DataAvailable){byte[] b=new byte[4096];int n=stream.Read(b,0,b.Length);if(n==0)throw new Exception("Ligação encerrada.");buffer+=Encoding.ASCII.GetString(b,0,n);Last=DateTime.UtcNow;if(buffer.Length>1048576)throw new Exception("Resposta demasiado longa.");continue;}if(Client.Client.Poll(0,SelectMode.SelectRead)&&Client.Available==0)throw new Exception("Ligação encerrada.");if(wait==0)return null;if(DateTime.UtcNow>=until)throw new Exception("Tempo de resposta excedido.");Thread.Sleep(10);}}
 public void Dispose(){if(Client!=null)Client.Close();Client=null;buffer="";}
}
public class Snapshot {
 public Fields Conf=new Fields(),Info=new Fields(),Network=new Fields(),Stack=new Fields();
 public List<Fields> Antennas=new List<Fields>(),Bands=new List<Fields>(),Flex=new List<Fields>();public string[] Outputs=new string[0],Groups=new string[0];
 public string BandName(int id){var b=Bands.FirstOrDefault(x=>x.Int("id")==id);return b==null?"None":b.Get("name").Replace('_',' ');}
 public string AntBands(Fields a){int mask=Convert.ToInt32(a.Get("rx"),16)|Convert.ToInt32(a.Get("tx"),16);return string.Join(" ",Bands.Where(b=>(mask&(1<<b.Int("id")))!=0).Select(b=>BandName(b.Int("id"))));}
}
public class AGClient : IDisposable {
 Wire wire=new Wire();int seq;public Fields[] Ports={new Fields(),new Fields()};public Snapshot Config;public bool Connected;public string Greeting;
 public void Connect(Profile p){Dispose();try{wire.Open(p.Address,p.Port);Greeting=wire.Read(5000);if(!Regex.IsMatch(Greeting,@"^V4\.\S+ AG(?: AUTH)?$"))throw new Exception("Equipamento não suportado: "+Greeting);if(Greeting.EndsWith(" AUTH")){string secret=p.Password();if(secret.Length==0)throw new Exception("Introduza a palavra-passe remota.");Command("auth code="+secret);}Command("sub port all");DateTime end=DateTime.UtcNow.AddSeconds(3);while(Ports.Any(f=>f.Count==0)&&DateTime.UtcNow<end){Drain();Thread.Sleep(20);}if(Ports.Any(f=>f.Count==0))throw new Exception("Estado inicial das portas indisponível.");Reload();Connected=true;}catch{Dispose();throw;}}
 void Status(string line){Match m=Regex.Match(line,@"^S\d+\|port ([12]) ");if(!m.Success)return;int p=int.Parse(m.Groups[1].Value)-1;var f=Fields.Parse(line);bool changed=f.Any(x=>new[]{"auto","source","band","rxant","txant"}.Contains(x.Key)&&Ports[p].Get(x.Key)!=x.Value);foreach(var pair in f)Ports[p][pair.Key]=pair.Value;if(changed)Storage.Log("ESTADO porta="+(p+1)+" source="+Ports[p].Get("source")+" band="+Ports[p].Get("band"));}
 public void Drain(){string line;while((line=wire.Read(0))!=null)Status(line);}
 public string[] Command(string command){seq=seq%255+1;int id=seq;wire.Send("C"+id+"|"+command);if(command.StartsWith("port set "))Storage.Log("ENVIADO "+command);var rows=new List<string>();DateTime end=DateTime.UtcNow.AddSeconds(6);while(DateTime.UtcNow<end){string line=wire.Read(5000);Status(line);Match m=Regex.Match(line,"^R"+id+@"\|([0-9A-Fa-f]+)\|(.*)$");if(!m.Success)continue;if(Convert.ToUInt32(m.Groups[1].Value,16)!=0)throw new Exception(command.StartsWith("auth ")?"Autenticação remota recusada.":"AG rejeitou "+command+" (código "+m.Groups[1].Value+")");string payload=m.Groups[2].Value;if(payload.Length>0)rows.Add(payload);if(!command.EndsWith(" list")||payload.Length==0)return rows.ToArray();}throw new Exception("Resposta incompleta.");}
 public Fields Get(string command){return Fields.Parse(Command(command).FirstOrDefault()??"");}
 List<Fields> List(string kind){return Command(kind+" list").Select(row=>{var f=Fields.Parse(row);Match m=Regex.Match(row,@"^\w+ (\d+) ");if(m.Success)f["id"]=m.Groups[1].Value;return f;}).ToList();}
 public void Reload(){var s=new Snapshot();s.Conf=Get("conf get");s.Info=Get("info get");s.Network=Get("network get");s.Network.Remove("auth");s.Stack=Get("stack get");s.Antennas=List("antenna");s.Bands=List("band");s.Flex=new[]{Get("flex get 1"),Get("flex get 2")}.ToList();s.Outputs=Command("output list");s.Groups=Command("group list");if(s.Antennas.Count!=8)throw new Exception("Esta versão requer oito antenas.");Config=s;}
 public void Poll(){Drain();if((DateTime.UtcNow-wire.Last).TotalSeconds>15)Command("ping");}
 public static string Mode(Fields f){return f.Get("auto")=="1"?"AUTO":f.Get("source");}
 public void Guard(){Drain();if(Ports.Any(p=>p.Get("tx")=="1"))throw new Exception("Alteração bloqueada durante transmissão.");}
 public void WaitMode(int port,string mode){DateTime end=DateTime.UtcNow.AddSeconds(2);do{Drain();if(Mode(Ports[port])==mode)return;Thread.Sleep(50);}while(DateTime.UtcNow<end);throw new Exception("Modo não confirmado: "+Mode(Ports[port]));}
 public void SetMode(int port,string mode){if(!new[]{"AUTO","FLEX","MANUAL"}.Contains(mode))throw new Exception("Modo inválido.");Guard();if(mode=="FLEX"&&Mode(Ports[port])=="MANUAL"&&Ports[port].Int("band")==0&&(Ports[port].Int("rxant")>0||Ports[port].Int("txant")>0)){Select(port,0);Guard();}if(mode=="FLEX"&&Ports[port].Get("auto")=="1"&&Ports[port].Get("source")=="FLEX"){Command("port set "+(port+1)+" source=MANUAL");WaitMode(port,"MANUAL");Guard();}Command("port set "+(port+1)+" source="+mode);WaitMode(port,mode);}
 public void Select(int port,int ant){Guard();int tx=0;string extra=" auto=0 source=MANUAL band=0";if(ant>0){var a=Config.Antennas.First(x=>x.Int("id")==ant);int rxMask=Convert.ToInt32(a.Get("rx"),16),txMask=Convert.ToInt32(a.Get("tx"),16),allowed=txMask==0?rxMask:rxMask&txMask;var bands=Enumerable.Range(0,16).Where(i=>(allowed&(1<<i))!=0).ToArray();if(bands.Length==0)throw new Exception("Antena sem bandas RX/TX compatíveis.");tx=txMask==0?0:ant;extra=bands.Contains(Ports[port].Int("band"))?"":" auto=0 source=MANUAL band="+bands[0];}Guard();if(ant>0&&(Ports[1-port].Int("rxant")==ant||Ports[1-port].Int("txant")==ant)){Select(1-port,0);Guard();if(Ports[1-port].Int("rxant")==ant||Ports[1-port].Int("txant")==ant)throw new Exception("A outra saída não confirmou a libertação da antena.");if(extra=="")extra=" auto=0 source=MANUAL";}Guard();Command("port set "+(port+1)+extra+" rxant="+ant+" txant="+tx);DateTime end=DateTime.UtcNow.AddSeconds(2);do{Drain();if(Ports[port].Int("rxant")==ant&&Ports[port].Int("txant")==tx)return;Thread.Sleep(50);}while(DateTime.UtcNow<end);throw new Exception("Seleção não confirmada.");}
 public void Apply(IEnumerable<string> commands){foreach(string cmd in commands){Guard();Command(cmd);string[] parts=cmd.Split(' ');Fields actual;if(parts[0]=="conf")actual=Get("conf get");else if(parts[0]=="flex")actual=Get("flex get "+parts[2]);else actual=List(parts[0]).First(x=>x.Get("id")==parts[2]);foreach(var f in Fields.Parse(cmd)){bool same=f.Key.StartsWith("freq_")?Validation.Frequency(actual.Get(f.Key))==Validation.Frequency(f.Value):actual.Get(f.Key)==f.Value;if(!same)throw new Exception("O equipamento não confirmou "+f.Key+". Algumas alterações podem ter sido guardadas.");}}Reload();}
 public void Dispose(){Connected=false;wire.Dispose();Ports=new[]{new Fields(),new Fields()};}
}
public static class Validation {
 public static string Name(string s){s=s.Trim().Replace(' ','_');if(!Regex.IsMatch(s,@"^[A-Za-z0-9_./()+-]{1,40}$"))throw new Exception("Nome inválido: use 1 a 40 letras sem acentos, números ou _ . / ( ) + -");return s;}
 public static string Frequency(string s){s=s.Trim().Replace(',','.');decimal d;if(!Regex.IsMatch(s,@"^\d+(\.\d{1,6})?$")||!decimal.TryParse(s,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out d))throw new Exception("Frequência inválida: use MHz com até seis casas decimais.");return d.ToString("0.000000",CultureInfo.InvariantCulture);}
 public static string IPv4(string s){s=s.Trim();if(!Regex.IsMatch(s,@"^(0|[1-9]\d{0,2})(\.(0|[1-9]\d{0,2})){3}$")||s.Split('.').Any(x=>int.Parse(x)>255))throw new Exception("Endereço IPv4 inválido.");return s;}
 public static string Network(Fields old,bool dhcp,string ip,string mask,string gw){if(dhcp)return old.Get("dhcp")=="1"?null:"network set dhcp=1";ip=IPv4(ip);mask=IPv4(mask);gw=IPv4(gw);int[] a=ip.Split('.').Select(int.Parse).ToArray(),m=mask.Split('.').Select(int.Parse).ToArray(),g=gw.Split('.').Select(int.Parse).ToArray();string bits=string.Concat(m.Select(x=>Convert.ToString(x,2).PadLeft(8,'0')));if(!Regex.IsMatch(bits,"^1{1,30}0{2,31}$"))throw new Exception("Máscara inválida.");if(a[0]==0||a[0]==127||a[0]>=224)throw new Exception("IP inválido para o equipamento.");if(Enumerable.Range(0,4).All(i=>(a[i]&(255^m[i]))==0)||Enumerable.Range(0,4).All(i=>(a[i]&(255^m[i]))==(255^m[i])))throw new Exception("IP de rede ou broadcast não permitido.");if(gw!="0.0.0.0"&&(gw==ip||g[0]==0||g[0]==127||g[0]>=224||Enumerable.Range(0,4).All(i=>(g[i]&(255^m[i]))==0)||Enumerable.Range(0,4).All(i=>(g[i]&(255^m[i]))==(255^m[i]))||Enumerable.Range(0,4).Any(i=>(a[i]&m[i])!=(g[i]&m[i]))))throw new Exception("Gateway inválido para esta rede.");if(old.Get("dhcp")=="0"&&old.Get("address")==ip&&old.Get("netmask")==mask&&old.Get("gateway")==gw)return null;return "network set dhcp=0 address="+ip+" netmask="+mask+" gateway="+gw;}
}
}
