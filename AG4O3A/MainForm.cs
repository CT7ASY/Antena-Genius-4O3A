using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace AG4O3A {
static class Program {
 public const string Version="1.0.5";public static bool Demo,Test,LiveTest;public static string TestOutput;
 [DllImport("user32.dll")]static extern bool SetProcessDPIAware();
 [STAThread]static void Main(string[] args){
  Test=args.Contains("--self-test")||args.Contains("--ui-test")||args.Contains("--live-test");Demo=args.Contains("--demo")||args.Contains("--ui-test");LiveTest=args.Contains("--live-test");TestOutput=args.LastOrDefault(a=>a.StartsWith("--output="));TestOutput=TestOutput==null?AppDomain.CurrentDomain.BaseDirectory:TestOutput.Substring(9);
  if(Test)Storage.Root=Path.Combine(TestOutput,"test-data");
  if(args.Contains("--self-test")){try{Tests.Run();File.WriteAllText(Path.Combine(TestOutput,"self-test.txt"),"PASS");}catch(Exception ex){File.WriteAllText(Path.Combine(TestOutput,"self-test.txt"),ex.ToString());Environment.ExitCode=1;}return;}
  bool owned=false;Mutex mutex=null;EventWaitHandle signal=null;
  try{
   SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Lang.Load();
   if(!Test){mutex=new Mutex(false,@"Local\CT7ASY.AG4O3A");try{owned=mutex.WaitOne(0);}catch(AbandonedMutexException){owned=true;}signal=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\CT7ASY.AG4O3A.Activate");if(!owned){signal.Set();MessageBox.Show(Lang.T("O programa já está aberto."),"AG 4O3A",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}}

   Profile p=Test?new Profile():Profile.Load();using(var form=new MainForm(p,signal)){Application.Run(form);}
  }catch(Exception ex){Storage.Log(ex.ToString());if(Test){File.WriteAllText(Path.Combine(TestOutput,"ui-error.txt"),ex.ToString());Environment.ExitCode=1;}else MessageBox.Show(ex.Message,"AG 4O3A",MessageBoxButtons.OK,MessageBoxIcon.Error);}
  finally{if(signal!=null)signal.Dispose();if(mutex!=null){if(owned)mutex.ReleaseMutex();mutex.Dispose();}}
 }
}
public partial class MainForm : Form {
 [DllImport("user32.dll")] static extern bool ReleaseCapture();
 [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
 public AGClient AG=new AGClient();public FlexFollow Follow=new FlexFollow();public Profile Profile;public bool Busy;bool backgroundUpdate;public SettingsForm Settings;
 System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer(),activate=new System.Windows.Forms.Timer();EventWaitHandle signal;
 AGClassicLabel title,footer;AGClassicLabel[] bandLabels=new AGClassicLabel[2],names=new AGClassicLabel[8],tags=new AGClassicLabel[8];AGClassicButton[] modes=new AGClassicButton[2];AGClassicButton[,] buttons=new AGClassicButton[2,8];ToolTip tips=new ToolTip();
 Color idle=Color.FromArgb(50,60,69),green=Color.FromArgb(0,168,0),blue=Color.FromArgb(0,157,235),transmitting=Color.FromArgb(215,35,35);public string Status="Sem ligação";
 public MainForm(Profile profile,EventWaitHandle wake){Profile=profile;signal=wake;Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);Text="AG 4O3A "+Program.Version;AutoScaleMode=AutoScaleMode.None;UI.ScaleOnLoad(this);ClientSize=new Size(320,586);BackColor=Color.Gray;Font=new Font("Segoe UI",10);FormBorderStyle=FormBorderStyle.None;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
  title=Label("AG 4O3A",40,3,240,36,12);title.Font=new Font("Segoe UI",12,FontStyle.Bold);title.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}};var windowMenu=new ContextMenuStrip();windowMenu.Items.Add(Lang.T("Minimizar"),null,delegate{WindowState=FormWindowState.Minimized;});windowMenu.Items.Add(Lang.T("Fechar"),null,delegate{Close();});title.ContextMenuStrip=windowMenu;var gear=Button("\u2699",3,3,36,36);tips.SetToolTip(gear,"Definições / voltar a ligar");gear.Click+=delegate{OpenSettings();};var exitButton=Button("\u00D7",281,3,36,36);exitButton.Font=new Font("Segoe UI",16,FontStyle.Bold);tips.SetToolTip(exitButton,Lang.T("Fechar"));exitButton.Click+=delegate{Close();};
  for(int p=0;p<2;p++){int port=p;modes[p]=Button("MANUAL",3+p*158,41,156,34);var menu=new ContextMenuStrip();foreach(string mode in new[]{"AUTO","FLEX","MANUAL"}){string chosen=mode;menu.Items.Add(mode,null,async delegate{await Run(()=>{if(Program.Demo){AG.Ports[port]["source"]=chosen;AG.Ports[port]["auto"]=chosen=="AUTO"?"1":"0";}else{AG.SetMode(port,chosen);Follow.ResetBaseline(AG.Ports);}});});}modes[p].Click+=delegate{menu.Show(modes[port],new Point(0,modes[port].Height));};modes[p].ContextMenuStrip=menu;bandLabels[p]=Label("None",3+p*158,77,156,32,10);}
  for(int i=0;i<8;i++){int ant=i+1,y=114+i*54;for(int p=0;p<2;p++){int port=p;buttons[p,i]=Button(p==0?"A":"B",p==0?3:285,y,32,50);buttons[p,i].BackColor=idle;buttons[p,i].Click+=async delegate{await SelectAntenna(port,ant);};}names[i]=Label("Antena "+ant,36,y,248,30,10);tags[i]=Label("None",36,y+30,248,20,8.5f);names[i].GradientHeight=tags[i].GradientHeight=50;tags[i].GradientOffset=30;}
  footer=Label("A ligar...",3,550,314,32,10.5f);
  timer.Interval=150;timer.Tick+=async delegate{if(Busy||!AG.Connected||Program.Demo||Program.Test)return;await Run(()=>{AG.Poll();Follow.Poll();for(int p=0;p<2;p++){bool tx=AG.Ports.Any(x=>x.Get("tx")=="1");if(Follow.ShouldResume(p,Follow.Observe(p),AG.Ports[p],tx)){AG.Guard();if(AGClient.Mode(AG.Ports[p])=="MANUAL"){AG.SetMode(p,"FLEX");Storage.Log("Regresso automático a FLEX porta="+(p+1));}}}},false,true);};
  activate.Interval=200;activate.Tick+=delegate{if(signal!=null&&signal.WaitOne(0)){Show();if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;Activate();BringToFront();}};activate.Start();
  Shown+=async delegate{for(int i=0;i<8;i++){names[i].GradientHeight=tags[i].GradientHeight=names[i].Height+tags[i].Height;tags[i].GradientOffset=names[i].Height;}if(Program.Demo){AG.Config=Tests.Fixture();AG.Connected=true;AG.Ports=new[]{Fields.Parse("auto=0 source=MANUAL band=5 rxant=2 txant=2 tx=0"),Fields.Parse("auto=0 source=MANUAL band=1 rxant=4 txant=4 tx=0")};Render();}else await Connect();timer.Start();if(Program.Test){try{if(!AG.Connected)throw new Exception("Teste sem ligação.");Capture(Path.Combine(Program.TestOutput,"main.png"));Settings=new SettingsForm(this);Settings.Show();Settings.CapturePages(Program.TestOutput);if(Program.Demo)Settings.CaptureLanguages(Program.TestOutput);Settings.Close();using(var dialog=new ConnectionForm(Profile)){dialog.Show();UI.Capture(dialog,Path.Combine(Program.TestOutput,"connection.png"));dialog.Close();}File.WriteAllText(Path.Combine(Program.TestOutput,"ui-test.txt"),"PASS: executable renders main and settings; no hardware writes");}catch(Exception ex){File.WriteAllText(Path.Combine(Program.TestOutput,"ui-error.txt"),ex.ToString());Environment.ExitCode=1;}Close();}};
  EnableWindowResize();Positions.Attach(this,"main",true);FormClosing+=PrepareExit;
  FormClosed+=delegate{timer.Stop();activate.Stop();if(Settings!=null&&!Settings.IsDisposed)Settings.Close();};
 }
 bool preparingExit,exitReady;
 void PrepareExit(object sender,FormClosingEventArgs e){
  if(exitReady || Program.Test || Program.Demo || e.CloseReason!=CloseReason.UserClosing) return;
  if(!AG.Connected && !Busy) return;
  e.Cancel=true;
  if(preparingExit) return;
  preparingExit=true;
  timer.Stop();
  BeginInvoke(new Action(FinishExit));
 }
 async void FinishExit(){
  try{
   while(Busy && !IsDisposed) await Task.Delay(50);
   if(IsDisposed) return;
   if(AG.Connected){
    Busy=true;
    Render();
    try{await Task.Run((Action)AG.AutoBeforeExit);}
    catch(Exception ex){
     Storage.Log(ex.Message);
     if(MessageBox.Show(this,ex.Message+"\n\n"+Lang.T("AUTO não confirmado. Sair mesmo assim?"),"AG 4O3A",
       MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes) return;
    }finally{Busy=false;if(!IsDisposed)Render();}
   }
   exitReady=true;
   Close();
  }finally{
   preparingExit=false;
   if(!exitReady && !IsDisposed) timer.Start();
  }
 }
 protected override void Dispose(bool disposing){if(disposing){timer.Dispose();activate.Dispose();tips.Dispose();Follow.Dispose();AG.Dispose();}base.Dispose(disposing);}
 AGClassicLabel Label(string text,int x,int y,int w,int h,float size){var l=new AGClassicLabel{Text=text,BackColor=Color.FromArgb(66,66,66),ForeColor=Color.White,Font=new Font("Segoe UI",size)};l.SetBounds(x,y,w,h);Controls.Add(l);return l;}
 AGClassicButton Button(string text,int x,int y,int w,int h){var b=new AGClassicButton{Text=text,BackColor=Color.FromArgb(66,66,66),ForeColor=Color.White};b.SetBounds(x,y,w,h);Controls.Add(b);return b;}
 public async Task<bool> Run(Action work,bool showError=true,bool background=false){if(Busy){if(showError)MessageBox.Show(this,"Aguarde a operação em curso.","AG 4O3A");return false;}Busy=true;backgroundUpdate=background;if(!background)Render();try{await Task.Run(work);return true;}catch(Exception ex){Storage.Log(ex.Message);Status=ex.Message;if(!showError&&!ex.Message.Contains("durante transmissão")){AG.Dispose();Follow.Dispose();}if(showError)MessageBox.Show(this,ex.Message,"AG 4O3A",MessageBoxButtons.OK,MessageBoxIcon.Warning);return false;}finally{Busy=false;backgroundUpdate=false;if(!IsDisposed)Render();}}
 public async Task Connect(){await Run(()=>{Follow.Dispose();AG.Connect(Profile);Follow.Reset(AG.Config);Status=Profile.Mode+" | "+Profile.Address+":"+Profile.Port;});}
 public async Task SelectAntenna(int port,int ant){await Run(()=>{int selected=ant>0&&AG.Ports[port].Int("rxant")==ant?0:ant;if(Program.Demo){if(selected>0&&(AG.Ports[1-port].Int("rxant")==selected||AG.Ports[1-port].Int("txant")==selected)){AG.Ports[1-port]["rxant"]="0";AG.Ports[1-port]["txant"]="0";AG.Ports[1-port]["band"]="0";AG.Ports[1-port]["auto"]="0";AG.Ports[1-port]["source"]="MANUAL";}AG.Ports[port]["rxant"]=selected.ToString();AG.Ports[port]["txant"]=selected.ToString();AG.Ports[port]["auto"]="0";AG.Ports[port]["source"]="MANUAL";if(selected==0)AG.Ports[port]["band"]="0";}else{try{AG.Select(port,selected);}finally{Follow.ResetBaseline(AG.Ports);}}});}
 public async void OpenSettings(){if(Settings!=null&&!Settings.IsDisposed){Settings.Show();if(Settings.WindowState==FormWindowState.Minimized)Settings.WindowState=FormWindowState.Normal;Settings.Activate();return;}if(Busy)return;if(!AG.Connected){using(var d=new ConnectionForm(Profile)){if(d.ShowDialog(this)==DialogResult.OK){Profile=d.Result;Profile.Save();await Connect();}}return;}if(!Program.Demo&&!await Run(()=>AG.Reload()))return;Settings=new SettingsForm(this);Settings.Show();}
 public void Render(){bool online=AG.Connected;string deviceName=online&&AG.Config!=null?AG.Config.Conf.Get("name").Replace('_',' ').Trim():"";title.Text=string.IsNullOrEmpty(deviceName)?"AG 4O3A":deviceName;Text="AG 4O3A "+Program.Version+(string.IsNullOrEmpty(deviceName)?"":" | "+deviceName);bool blocked=(Busy&&!backgroundUpdate)||!online||AG.Ports.Any(x=>x.Get("tx")=="1");for(int p=0;p<2;p++){modes[p].Enabled=!blocked;modes[p].Text=online?AGClient.Mode(AG.Ports[p]):"MANUAL";foreach(ToolStripMenuItem m in modes[p].ContextMenuStrip.Items)m.Checked=m.Text==modes[p].Text;bandLabels[p].Text=online?AG.Config.BandName(AG.Ports[p].Int("band")):Lang.T("Sem ligação");for(int i=0;i<8;i++){buttons[p,i].Enabled=!blocked;buttons[p,i].BackColor=PortAntennaColor(p,i+1);}}
  for(int i=0;i<8;i++){if(AG.Config!=null){var a=AG.Config.Antennas.First(x=>x.Int("id")==i+1);names[i].Text=a.Get("name").Replace('_',' ');tags[i].Text=AG.Config.AntBands(a);bool hasBand=((Convert.ToInt32(a.Get("rx"),16)|Convert.ToInt32(a.Get("tx"),16))&0xFFFE)!=0;names[i].ForeColor=tags[i].ForeColor=hasBand?Color.White:Color.FromArgb(165,174,181);}Color color=idle;Color aColor=PortAntennaColor(0,i+1),bColor=PortAntennaColor(1,i+1);if(aColor==transmitting||bColor==transmitting)color=transmitting;else if(aColor!=idle)color=aColor;else color=bColor;names[i].BackColor=tags[i].BackColor=color;}footer.Text=Program.Demo?"Demonstração":online?Lang.T(Profile.Mode)+"  "+Profile.Address+":"+Profile.Port:Lang.T("Sem ligação")+" | "+Lang.T("Definições");tips.SetToolTip(footer,Status);
 }
 Color PortAntennaColor(int port,int antenna){
  if(!AG.Connected) return idle;
  Fields state=AG.Ports[port];
  if(state.Get("tx")=="1" && state.Int("txant")==antenna) return transmitting;
  if(state.Int("rxant")==antenna) return port==0?green:blue;
  return idle;
 }
 public new void Capture(string path){using(var b=new Bitmap(Width,Height)){DrawToBitmap(b,new Rectangle(0,0,Width,Height));b.Save(path);}}
}
}
