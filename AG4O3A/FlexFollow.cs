using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
namespace AG4O3A {
public class FlexFollow : IDisposable {
 class Radio {public Wire Wire=new Wire();public string Serial="";public bool Ready;public long Session=DateTime.UtcNow.Ticks;public Dictionary<int,Fields> Slices=new Dictionary<int,Fields>();public int Seq=2;public DateTime Ping=DateTime.UtcNow;}
 public class Observation {public int Band;public string Identity;}
 public class Track {public string Identity;public int Band;public bool Pending;public DateTime Last;}
 public Dictionary<int,Track> Tracks=new Dictionary<int,Track>();
 Dictionary<string,Radio> radios=new Dictionary<string,Radio>();Dictionary<string,DateTime> failed=new Dictionary<string,DateTime>();UdpClient discovery;Snapshot config;DateTime discoverNext=DateTime.MinValue;
 public void Reset(Snapshot s){Dispose();config=s;if(!s.Flex.Any(x=>x.Get("serial")!=""))return;try{discovery=new UdpClient();discovery.ExclusiveAddressUse=false;discovery.Client.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress,true);discovery.Client.Bind(new IPEndPoint(IPAddress.Any,4992));}catch{if(discovery!=null)discovery.Close();discovery=null;}}
 void Add(string address,int port){string key=address+":"+port;if(radios.ContainsKey(key)||(failed.ContainsKey(key)&&failed[key]>DateTime.UtcNow))return;var r=new Radio();try{r.Wire.Open(address,port);r.Wire.Send("C1|info");radios[key]=r;}catch{r.Wire.Dispose();failed[key]=DateTime.UtcNow.AddSeconds(15);}}
 public void Poll(){if(config==null||!config.Flex.Any(x=>x.Get("serial")!=""))return;
  if(discovery!=null)for(int i=0;i<20&&discovery.Available>0;i++){var peer=new IPEndPoint(IPAddress.Any,0);string text=Encoding.ASCII.GetString(discovery.Receive(ref peer));int start=text.IndexOf("model=");if(start<0)continue;Fields f=Fields.Parse(text.Substring(start).TrimEnd('\0'));if(config.Flex.Any(x=>x.Get("serial")==f.Get("serial")&&f.Get("serial")!=""))Add(peer.Address.ToString(),f.Int("port")>0?f.Int("port"):4992);}
  if(DateTime.UtcNow>discoverNext){discoverNext=DateTime.UtcNow.AddSeconds(10);foreach(var c in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections())if(c.State==TcpState.Established&&c.RemoteEndPoint.Port==4992)Add(c.RemoteEndPoint.Address.ToString(),4992);}
  foreach(string key in radios.Keys.ToArray()){var r=radios[key];try{string line;int count=0;while(count++<1000&&(line=r.Wire.Read(0))!=null){Match m=Regex.Match(line,"^R1\\|0\\|.*chassis_serial=\"([^\"]+)\"");if(m.Success){r.Serial=m.Groups[1].Value;if(!config.Flex.Any(f=>f.Get("serial")==r.Serial))throw new Exception("Rádio não associado.");r.Wire.Send("C2|sub slice all");continue;}m=Regex.Match(line,@"^R2\|([0-9A-Fa-f]+)\|");if(m.Success){if(Convert.ToUInt32(m.Groups[1].Value,16)!=0)throw new Exception("Subscrição Flex recusada.");r.Ready=true;continue;}m=Regex.Match(line,@"^S[0-9A-Fa-f]+\|slice (\d+) (.*)$");if(!m.Success)continue;int id=int.Parse(m.Groups[1].Value);string data=m.Groups[2].Value;if(Regex.IsMatch(data,@"(^| )removed($| )")){r.Slices.Remove(id);continue;}if(!r.Slices.ContainsKey(id))r.Slices[id]=new Fields();foreach(var p in Fields.Parse(data))r.Slices[id][p.Key]=p.Value;}
   if((DateTime.UtcNow-r.Wire.Last).TotalSeconds>30||(!r.Ready&&(DateTime.UtcNow.Ticks-r.Session)>TimeSpan.FromSeconds(6).Ticks))throw new Exception("Flex sem resposta.");if((DateTime.UtcNow-r.Ping).TotalSeconds>10){r.Wire.Send("C"+(++r.Seq)+"|ping");r.Ping=DateTime.UtcNow;}
  }catch(Exception ex){Storage.Log("Flex: "+ex.Message);r.Wire.Dispose();radios.Remove(key);failed[key]=DateTime.UtcNow.AddSeconds(10);}}
 }
 public Observation Observe(int p){if(config==null)return null;var binding=config.Flex[p];if(binding.Get("serial")=="")return null;var rs=radios.Values.Where(r=>r.Ready&&r.Serial==binding.Get("serial")).ToArray();if(rs.Length!=1)return null;var radio=rs[0];string ant=binding.Get("ant").Replace("XVRT","XVTA");var slices=radio.Slices.Values.Where(x=>x.Get("in_use")=="1"&&x.Get("RF_frequency")!="").ToArray();var selected=slices.Where(x=>x.Get("tx")=="1"&&x.Get("txant")==ant).ToArray();if(selected.Length==0)selected=slices.Where(x=>x.Get("rxant")==ant).ToArray();if(selected.Length!=1)return null;double freq;if(!double.TryParse(selected[0].Get("RF_frequency"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out freq))return null;int band=0;foreach(var b in config.Bands){double lo=double.Parse(b.Get("freq_start"),System.Globalization.CultureInfo.InvariantCulture),hi=double.Parse(b.Get("freq_stop"),System.Globalization.CultureInfo.InvariantCulture);if(b.Int("id")>0&&hi>lo&&freq>=lo&&freq<=hi){band=b.Int("id");break;}}return new Observation{Band=band,Identity=radio.Serial+"|"+ant+"|"+radio.Session};}
 public bool ShouldResume(int p,Observation o,Fields state,bool tx){Track t;DateTime now=DateTime.UtcNow;bool exists=Tracks.TryGetValue(p,out t);if(o==null||o.Band==0){if(exists&&(now-t.Last).TotalSeconds>5)Tracks.Remove(p);return false;}if(!exists||t.Identity!=o.Identity||(now-t.Last).TotalSeconds>5){t=new Track{Band=o.Band,Identity=o.Identity,Last=now,Pending=AGClient.Mode(state)=="MANUAL"};Tracks[p]=t;}t.Last=now;if(AGClient.Mode(state)!="MANUAL"){t.Band=o.Band;t.Pending=false;return false;}if(t.Band!=o.Band){t.Band=o.Band;t.Pending=true;}if(t.Pending&&!tx){t.Pending=false;return true;}return false;}
 public void ResetBaseline(Fields[] ports){Tracks.Clear();Poll();for(int i=0;i<2;i++){var o=Observe(i);if(o!=null&&o.Band>0)Tracks[i]=new Track{Band=o.Band,Identity=o.Identity,Last=DateTime.UtcNow,Pending=false};}}
 public void Dispose(){foreach(var r in radios.Values)r.Wire.Dispose();radios.Clear();failed.Clear();Tracks.Clear();if(discovery!=null)discovery.Close();discovery=null;discoverNext=DateTime.MinValue;}
}
}
