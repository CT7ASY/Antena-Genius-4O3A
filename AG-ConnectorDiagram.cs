using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

// Draw at the current control resolution, never from a scaled screenshot.
public class AGConnectorDiagram : Control {
 public bool P2P {get;set;}
 private string language="pt";
 public string Language {get{return language;}set{language=value;Invalidate();}}
 public AGConnectorDiagram(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);BackColor=Color.White;}
 string T(string pt,string es,string en){return language=="es"?es:language=="en"?en:pt;}
 static readonly Color Ink=Color.FromArgb(40,54,68),Accent=Color.FromArgb(34,94,131),Grid=Color.FromArgb(183,195,204);
 void TextAt(Graphics g,string text,float x,float y,float w,float h,float size,bool bold,bool left){
  using(var font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))
  using(var brush=new SolidBrush(Ink))using(var format=new StringFormat()){
   format.Alignment=left?StringAlignment.Near:StringAlignment.Center;format.LineAlignment=StringAlignment.Center;
   g.DrawString(text,font,brush,new RectangleF(x,y,w,h),format);
  }
 }
 PointF Pin(int number,float x,float y){int row=(number-1)/5;int col=4-(number-1)%5;return new PointF(x+120+52*col,y+48+36*row);}
 void Connector(Graphics g,float x,float y){
  using(var pen=new Pen(Ink,3))using(var line=new Pen(Grid,2)){
   g.FillRectangle(Brushes.White,x,y,460,165);g.DrawRectangle(pen,x,y,460,165);
   g.DrawEllipse(pen,x+24,y+64,36,36);g.DrawEllipse(pen,x+400,y+64,36,36);
   using(var path=new GraphicsPath()){
    path.AddBezier(x+80,y+20,x+70,y+20,x+70,y+32,x+74,y+48);
    path.AddLine(x+74,y+48,x+92,y+132);path.AddBezier(x+92,y+132,x+94,y+146,x+106,y+148,x+120,y+148);
    path.AddLine(x+120,y+148,x+340,y+148);path.AddBezier(x+340,y+148,x+354,y+148,x+366,y+146,x+368,y+132);
    path.AddLine(x+368,y+132,x+386,y+48);path.AddBezier(x+386,y+48,x+390,y+32,x+390,y+20,x+380,y+20);
    path.CloseFigure();g.DrawPath(pen,path);
   }
   for(int n=1;n<=15;n++){PointF p=Pin(n,x,y);g.FillEllipse(Brushes.White,p.X-13,p.Y-13,26,26);g.DrawEllipse(line,p.X-13,p.Y-13,26,26);TextAt(g,n.ToString(),p.X-14,p.Y-14,28,28,18,true,false);}
  }
 }
 void Callout(Graphics g,PointF pin,string text,float x,float y,float width,bool above){
  using(var pen=new Pen(Accent,1.8f)){
   g.DrawLine(pen,pin.X,pin.Y+(above?-14:14),x+width/2,y+(above?44:0));
   using(var fill=new SolidBrush(Color.FromArgb(237,244,248)))g.FillRectangle(fill,x,y,width,44);
   g.DrawRectangle(pen,x,y,width,44);
  }
  TextAt(g,text,x+2,y,width-4,44,20,true,false);
 }
 protected override void OnPaint(PaintEventArgs e){
  base.OnPaint(e);var g=e.Graphics;g.Clear(Color.White);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
  float scale=Math.Min(Width/900f,Height/760f);g.TranslateTransform((Width-900*scale)/2,(Height-760*scale)/2);g.ScaleTransform(scale,scale);
  TextAt(g,T("Vista de frente do conector no equipamento","Vista frontal del conector del equipo","Front view of the connector on the device"),20,5,860,46,24,true,false);
  if(P2P){DrawP2P(g);return;}
  Connector(g,220,70);
  string band=T("Banda ","Banda ","Band ");string[] labels={"GND · 15",band+"D · 14",band+"C · 13",band+"B · 12",band+"A · 11"};
  for(int i=0;i<5;i++)Callout(g,Pin(15-i,220,70),labels[i],20+176*i,272,156,false);
  TextAt(g,T("Tabela de códigos BCD","Tabla de códigos BCD","BCD code table"),20,330,860,38,25,true,true);
  string[] bands={"160 m","80 m","40 m","30 m","20 m","17 m","15 m","12 m","10 m","6 m",T("Sem banda","Sin banda","None")};
  string[] freq={"1,8","3,5","7","10","14","18","21","24","28","50","—"};
  string[] rows={T("Banda","Banda","Band"),"MHz",band+"A",band+"B",band+"C",band+"D",T("Cód. HEX","Cód. HEX","HEX code")};
  for(int r=0;r<7;r++)for(int c=0;c<12;c++){
   float x=c==0?20:134+(c-1)*68;float w=c==0?114:68;float y=374+r*43;
   using(var brush=new SolidBrush(r==0?Color.FromArgb(215,229,239):r%2==0?Color.FromArgb(241,245,248):Color.White))g.FillRectangle(brush,x,y,w,43);
   using(var pen=new Pen(Grid,1))g.DrawRectangle(pen,x,y,w,43);
   string value;
   if(c==0)value=rows[r];else{int code=c==11?0:c;value=r==0?bands[c-1]:r==1?freq[c-1]:r==6?code.ToString("X"):((code&(1<<(r-2)))!=0?"H":"L");if(language=="en")value=value.Replace(',','.');}
   TextAt(g,value,x+2,y,w-4,43,c==11&&r==0?15:19,r==0||r==6,false);
  }
  TextAt(g,T("H = nível alto   ·   L = nível baixo   ·   GND = massa","H = nivel alto   ·   L = nivel bajo   ·   GND = masa","H = high level   ·   L = low level   ·   GND = ground"),20,686,860,28,20,false,true);
  TextAt(g,T("A banda de 60 m não é selecionável por BCD.","La banda de 60 m no se puede seleccionar por BCD.","The 60 m band cannot be selected via BCD."),20,720,860,28,20,false,true);
 }
 void DrawP2P(Graphics g){
  Connector(g,160,240);string antenna=T("Antena ","Antena ","Antenna ");
  for(int i=0;i<5;i++)Callout(g,Pin(5-i,160,240),antenna+(5-i),20+176*i,120,150,true);
  for(int n=6;n<=8;n++){
   PointF pin=Pin(n,160,240);float y=270+(n-6)*82;
   using(var pen=new Pen(Accent,1.8f))g.DrawLine(pen,pin.X+14,pin.Y,700,y+22);
   using(var fill=new SolidBrush(Color.FromArgb(237,244,248)))g.FillRectangle(fill,700,y,170,44);
   using(var pen=new Pen(Accent,1.8f))g.DrawRectangle(pen,700,y,170,44);
   TextAt(g,antenna+n,700,y,170,44,22,true,false);
  }
  Callout(g,Pin(15,160,240),"GND · 15",160,510,170,false);
  TextAt(g,T("Pinos 1–8: antenas 1–8","Pines 1–8: antenas 1–8","Pins 1–8: antennas 1–8"),20,610,860,38,26,true,false);
  TextAt(g,T("Pino 15: massa (GND)","Pin 15: masa (GND)","Pin 15: ground (GND)"),20,658,860,38,24,false,false);
 }
}
