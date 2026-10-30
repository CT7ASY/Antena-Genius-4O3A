
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
public static class AGRender {
 [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
 public static void EnableDpi(){try{SetProcessDPIAware();}catch{}}
 public static void Text(Graphics g,string text,Font font,Color color,Rectangle area){
  g.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
  using(var brush=new SolidBrush(color))using(var format=new StringFormat()){
   format.Alignment=StringAlignment.Center;format.LineAlignment=StringAlignment.Center;
   format.Trimming=StringTrimming.EllipsisCharacter;format.FormatFlags=StringFormatFlags.NoWrap;
   g.DrawString(text,font,brush,area,format);
  }
 }
 public static void Gradient(Graphics g,Rectangle fill,Rectangle gradient,Color color,float light,float dark){
  using(var brush=new LinearGradientBrush(gradient,ControlPaint.Light(color,light),ControlPaint.Dark(color,dark),90f)){
   brush.GammaCorrection=true;g.FillRectangle(brush,fill);
  }
 }
 public static void Gear(Graphics g,Rectangle bounds,Color color){
  g.SmoothingMode=SmoothingMode.AntiAlias;
  float cx=bounds.Width/2f,cy=bounds.Height/2f,r=Math.Min(bounds.Width,bounds.Height)*.22f;
  using(var pen=new Pen(color,Math.Max(1.5f,r*.3f))){
   g.DrawEllipse(pen,cx-r*.72f,cy-r*.72f,r*1.44f,r*1.44f);
   for(int i=0;i<8;i++){double a=i*Math.PI/4;g.DrawLine(pen,cx+(float)Math.Cos(a)*r*.8f,cy+(float)Math.Sin(a)*r*.8f,cx+(float)Math.Cos(a)*r*1.2f,cy+(float)Math.Sin(a)*r*1.2f);}
  }
 }
}
// Settings use the same native GDI text renderer as edit boxes and checkboxes.
public class AGSettingsLabel : Label {
 public AGSettingsLabel(){UseCompatibleTextRendering=false;TextAlign=ContentAlignment.MiddleLeft;}
}
public class AGChecklistButton : Button {
 public AGChecklistButton(){SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint,true);}
 protected override void OnPaint(PaintEventArgs e){
  e.Graphics.Clear(Color.FromArgb(237,239,245));
  using(var border=new Pen(Color.FromArgb(144,157,181)))e.Graphics.DrawRectangle(border,0,0,Width-1,Height-1);
  e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
  float k=Height/30f;
  using(var pen=new Pen(Enabled?Color.FromArgb(55,62,70):Color.Gray,1.7f*k)){
   for(int i=0;i<3;i++){
    float y=(7+i*8)*k;
    e.Graphics.DrawLines(pen,new PointF[]{new PointF(12*k,y),new PointF(14*k,y+2*k),new PointF(18*k,y-3*k)});
    e.Graphics.DrawLine(pen,22*k,y,Width-10*k,y);
   }
  }
  if(Focused && ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(3,3,Width-6,Height-6));
 }
}
public class AGClassicButton : Button {
 public AGClassicButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.ResizeRedraw,true);}
 protected override void OnPaint(PaintEventArgs e){
  if(Width<1 || Height<1)return;
  AGRender.Gradient(e.Graphics,ClientRectangle,ClientRectangle,BackColor,.3f,.32f);
  using(var pen=new Pen(Color.FromArgb(150,150,150)))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);
  Color foreground=ForeColor; // Keep text stable while a command temporarily blocks interaction.
  if(Text=="\u2699")AGRender.Gear(e.Graphics,ClientRectangle,foreground);
  else AGRender.Text(e.Graphics,Text,Font,foreground,ClientRectangle);
  if(Focused && ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(3,3,Width-6,Height-6));
 }
}
public class AGClassicLabel : Label {
 public int GradientHeight {get;set;}
 public int GradientOffset {get;set;}
 public AGClassicLabel(){SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.ResizeRedraw,true);}
 protected override void OnPaint(PaintEventArgs e){
  if(Width<1 || Height<1)return;
  if(BackColor != Color.Transparent){var area=new Rectangle(0,-GradientOffset,Width,GradientHeight>0?GradientHeight:Height);AGRender.Gradient(e.Graphics,ClientRectangle,area,BackColor,.45f,.28f);}
  AGRender.Text(e.Graphics,Text,Font,ForeColor,ClientRectangle);
 }
}
