using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace AG4O3A {
public static class Lang {
 public static string Code="pt";public static event Action Changed;
 static Dictionary<string,string[]> map=new Dictionary<string,string[]>();static Dictionary<Control,string> captions=new Dictionary<Control,string>();
 public static void Load(){string path=Storage.PathFor("language.txt");if(File.Exists(path)){string c=File.ReadAllText(path).Trim();if(new[]{"pt","es","en"}.Contains(c))Code=c;}string file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"languages.json");if(File.Exists(file))foreach(var row in new JavaScriptSerializer().Deserialize<string[][]>(File.ReadAllText(file)))map[row[0]]=new[]{row[1],row[2]};}
 public static string T(string text){string[] v;if(Code!="pt"&&map.TryGetValue(text,out v))return v[Code=="es"?0:1];foreach(string prefix in new[]{"Versão ","Série: ","FlexRadio | Porta "})if(text.StartsWith(prefix)&&text!=prefix)return T(prefix)+text.Substring(prefix.Length);return text;}
 public static void Bind(Control c,string text){captions[c]=text;c.Text=T(text);c.Disposed+=delegate{captions.Remove(c);};}
 public static void Set(string code){if(!new[]{"pt","es","en"}.Contains(code))throw new Exception("Idioma inválido.");string path=Storage.PathFor("language.txt"),tmp=path+".tmp";File.WriteAllText(tmp,code);if(File.Exists(path))File.Replace(tmp,path,null);else File.Move(tmp,path);Code=code;foreach(var p in captions.ToArray())if(!p.Key.IsDisposed)p.Key.Text=T(p.Value);if(Changed!=null)Changed();}
}
public static class Positions {
 public static Dictionary<string,Dictionary<string,int>> Read(){try{return new JavaScriptSerializer().Deserialize<Dictionary<string,Dictionary<string,int>>>(File.ReadAllText(Storage.PathFor("windows.json")))??new Dictionary<string,Dictionary<string,int>>();}catch{return new Dictionary<string,Dictionary<string,int>>();}}
 public static void Attach(Form f,string key,bool rememberSize=false){if(Program.Test)return;f.Load+=delegate{var all=Read();Dictionary<string,int> pos;if(!all.TryGetValue(key,out pos)||!pos.ContainsKey("x")||!pos.ContainsKey("y"))return;var area=Screen.FromRectangle(new Rectangle(pos["x"],pos["y"],f.Width,f.Height)).WorkingArea;if(rememberSize&&pos.ContainsKey("width")&&pos.ContainsKey("height"))f.Size=new Size(Math.Max(f.MinimumSize.Width,Math.Min(pos["width"],area.Width)),Math.Max(f.MinimumSize.Height,Math.Min(pos["height"],area.Height)));f.StartPosition=FormStartPosition.Manual;f.Location=new Point(Math.Max(area.Left,Math.Min(pos["x"],area.Right-f.Width)),Math.Max(area.Top,Math.Min(pos["y"],area.Bottom-f.Height)));};Action save=delegate{try{Rectangle r=f.WindowState==FormWindowState.Normal?f.Bounds:f.RestoreBounds;if(r.Width<=0)return;var all=Read();all[key]=new Dictionary<string,int>{{"x",r.X},{"y",r.Y}};if(rememberSize){all[key]["width"]=r.Width;all[key]["height"]=r.Height;}string path=Storage.PathFor("windows.json"),tmp=path+".tmp";File.WriteAllText(tmp,new JavaScriptSerializer().Serialize(all));if(File.Exists(path))File.Replace(tmp,path,null);else File.Move(tmp,path);}catch(Exception ex){Storage.Log("Posição: "+ex.Message);}};f.ResizeEnd+=delegate{save();};f.FormClosing+=delegate{save();};}
}
public class Tabs : TabControl {
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
 public Tabs(){SizeMode=TabSizeMode.Normal;Multiline=true;Padding=new Point(8,6);}
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);SendMessage(Handle,0x1331,IntPtr.Zero,IntPtr.Zero);}
}
public static class UI {
 public static Color Background=Color.FromArgb(214,220,226),Page=Color.FromArgb(229,233,237),Ink=Color.FromArgb(45,55,66),Header=Color.FromArgb(51,64,77);
 public static void Form(Form f,string caption,Size size){f.Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);f.AutoScaleMode=AutoScaleMode.None;ScaleOnLoad(f);f.ClientSize=size;f.Font=new Font("Segoe UI Semibold",10);f.BackColor=Background;f.ForeColor=Ink;f.StartPosition=FormStartPosition.CenterScreen;f.FormBorderStyle=FormBorderStyle.FixedDialog;f.MaximizeBox=false;Lang.Bind(f,caption);}
 public static void ScaleOnLoad(Form form){form.Load+=delegate{using(var g=form.CreateGraphics()){float scale=g.DpiX/96f;if(scale>1.001f)form.Scale(new SizeF(scale,scale));}};}
 public static Label Label(Control p,string text,int x,int y,int w=440,int h=26){var c=new Label{ForeColor=Ink,UseCompatibleTextRendering=false};c.SetBounds(x,y,w,h);p.Controls.Add(c);Lang.Bind(c,text);return c;}
 public static Label Head(Control p,string text,int y=0){var c=Label(p,text,0,y,494,28);c.BackColor=Header;c.ForeColor=Color.White;c.Font=new Font("Segoe UI",10,FontStyle.Bold);c.Padding=new Padding(10,3,0,0);return c;}
 public static TextBox Text(Control p,string value,int x,int y,int w=280){var c=new TextBox{Text=value,BorderStyle=BorderStyle.FixedSingle,BackColor=Color.FromArgb(243,245,247)};c.SetBounds(x,y,w,28);p.Controls.Add(c);return c;}
 public static Button Button(Control p,string text,int x,int y,int w=120,int h=32){var c=new Button{FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(247,249,251)};c.FlatAppearance.BorderColor=Color.FromArgb(199,207,215);c.SetBounds(x,y,w,h);p.Controls.Add(c);Lang.Bind(c,text);return c;}
 public static CheckBox Check(Control p,string text,bool value,int x,int y,int w=440){var c=new CheckBox{Checked=value};c.SetBounds(x,y,w,28);p.Controls.Add(c);Lang.Bind(c,text);return c;}
 public static ComboBox Combo(Control p,string[] choices,string value,int x,int y,int w=160){var c=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};c.Items.AddRange(choices);c.SetBounds(x,y,w,28);p.Controls.Add(c);c.SelectedItem=value;if(c.SelectedIndex<0&&choices.Length>0)c.SelectedIndex=0;return c;}
 public static void Capture(Form f,string path){Application.DoEvents();using(var image=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(image,new Rectangle(Point.Empty,f.Size));image.Save(path);}}
 public static void Error(IWin32Window owner,Exception ex){MessageBox.Show(owner,ex.Message,"AG 4O3A",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
}
public class ConnectionControls {
 public CheckBox Local,Remote;public TextBox Host,Password;public NumericUpDown Port;public Profile Original;bool changing,split;string localHost,remoteHost;int localPort,remotePort;
 public ConnectionControls(Control p,Profile profile,int top,bool separate){Original=profile.Copy();split=separate;localHost=profile.LocalHost;remoteHost=profile.RemoteHost;localPort=profile.LocalPort;remotePort=profile.RemotePort;
  UI.Head(p,separate?"Remota":"Ligação local / remota",top);
  Local=UI.Check(p,"Local",!profile.Remote,16,separate?34:top+38,100);Remote=UI.Check(p,"Remota",profile.Remote,separate?16:132,separate?top+58:top+38,110);
  UI.Label(p,"Palavra-passe remota:",152,top+34,330);Password=UI.Text(p,"",152,top+60,326);Password.UseSystemPasswordChar=true;try{Password.Text=profile.Password();}catch{}
  UI.Label(p,"Hostname / IP:",16,top+100,360);Host=UI.Text(p,separate?remoteHost:profile.Address,16,top+126,368);UI.Label(p,"TCP:",398,top+100,80);Port=new NumericUpDown{Minimum=1,Maximum=65535,Value=separate?remotePort:profile.Port};Port.SetBounds(398,top+126,80,28);p.Controls.Add(Port);
  UI.Label(p,"Guardada com proteção do Windows. A ligação muda ao guardar.",16,top+166,460,42);
  if(!separate){Local.Top=16;Remote.Top=16;}Local.CheckedChanged+=delegate{Switch(false);};Remote.CheckedChanged+=delegate{Switch(true);};Enable();
 }
 void Switch(bool remote){if(changing)return;changing=true;try{if(!(remote?Remote.Checked:Local.Checked)){(remote?Remote:Local).Checked=true;return;}if(!split){if(Original.Remote){remoteHost=Host.Text;remotePort=(int)Port.Value;}else{localHost=Host.Text;localPort=(int)Port.Value;}Original.Mode=remote?"Remota":"Local";Host.Text=remote?remoteHost:localHost;Port.Value=remote?remotePort:localPort;}Local.Checked=!remote;Remote.Checked=remote;Enable();}finally{changing=false;}}
 void Enable(){Password.Enabled=Remote.Checked;if(split){Host.Enabled=Port.Enabled=Remote.Checked;}}
 public Profile Read(){var p=Original.Copy();p.Mode=Remote.Checked?"Remota":"Local";p.LocalHost=localHost;p.LocalPort=localPort;p.RemoteHost=remoteHost;p.RemotePort=remotePort;if(split||p.Remote){p.RemoteHost=Host.Text.Trim();p.RemotePort=(int)Port.Value;}else{p.LocalHost=Host.Text.Trim();p.LocalPort=(int)Port.Value;}p.Validate();string old="";try{old=p.Password();}catch{}if(old!=Password.Text)p.SetPassword(Password.Text);return p;}
}
public class ConnectionForm : Form {
 public Profile Result;ConnectionControls controls;
 public ConnectionForm(Profile p){UI.Form(this,"AG 4O3A | Ligação",new Size(510,382));controls=new ConnectionControls(this,p,68,false);var save=UI.Button(this,"Guardar e ligar",238,338,150);save.Click+=delegate{try{Result=controls.Read();DialogResult=DialogResult.OK;Close();}catch(Exception ex){UI.Error(this,ex);}};var cancel=UI.Button(this,"Cancelar",396,338,106);cancel.Click+=delegate{Close();};CancelButton=cancel;Positions.Attach(this,"connection");}
}
}
