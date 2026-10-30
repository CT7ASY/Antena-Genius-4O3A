using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace AG4O3A {
public partial class MainForm {
 readonly Dictionary<Control,Rectangle> layoutBounds=new Dictionary<Control,Rectangle>();
 readonly List<Panel> resizeEdges=new List<Panel>();
 readonly Dictionary<Control,Font> originalFonts=new Dictionary<Control,Font>();
 readonly Dictionary<Control,Font> scaledFonts=new Dictionary<Control,Font>();
 float windowDpiScale=1f;



 [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
 bool nativeRoundedCorners;
 bool layoutReady;
 int edgeSize=4;
 void EnableWindowResize(){
  DoubleBuffered=true;
  foreach(Control c in Controls){layoutBounds[c]=c.Bounds;originalFonts[c]=c.Font;}
  Disposed+=delegate{foreach(var font in scaledFonts.Values)font.Dispose();};

  Cursor[] cursors={Cursors.SizeWE,Cursors.SizeWE,Cursors.SizeNS,Cursors.SizeNWSE,Cursors.SizeNESW,Cursors.SizeNS,Cursors.SizeNESW,Cursors.SizeNWSE};
  for(int i=0;i<8;i++){
   int direction=i+1;
   var edge=new Panel{BackColor=BackColor,Cursor=cursors[i],TabStop=false};
   edge.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0x112,new IntPtr(0xF000+direction),IntPtr.Zero);}};
   edge.Paint+=delegate(object sender,PaintEventArgs e){var state=e.Graphics.Save();e.Graphics.TranslateTransform(-edge.Left,-edge.Top);DrawWindowOutline(e.Graphics);e.Graphics.Restore(state);};Controls.Add(edge);resizeEdges.Add(edge);
  }
  Load+=delegate{
   using(var g=CreateGraphics()){float dpi=g.DpiX/96f;windowDpiScale=dpi;edgeSize=Math.Max(4,(int)Math.Round(4*dpi));MinimumSize=new Size((int)Math.Round(240*dpi),(int)Math.Round(400*dpi));}
   try{int preference=2;nativeRoundedCorners=DwmSetWindowAttribute(Handle,33,ref preference,sizeof(int))==0;}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}
   layoutReady=true;LayoutWindow();
  };
  ClientSizeChanged+=delegate{if(layoutReady&&WindowState!=FormWindowState.Minimized)LayoutWindow();};
 }
 void LayoutWindow(){
  SuspendLayout();
  try{
   int contentInset=(int)Math.Ceiling(3f*Math.Max(1.5f,edgeSize*.375f))+1;
   float sx=(ClientSize.Width-2*contentInset)/314f,sy=(ClientSize.Height-2*contentInset)/579f;

   float textScale=Math.Max(.85f,Math.Min(ClientSize.Width/(320f*windowDpiScale),ClientSize.Height/(586f*windowDpiScale)));
   foreach(var item in layoutBounds){Rectangle r=item.Value;int x=contentInset+(int)Math.Round((r.Left-3)*sx),y=contentInset+(int)Math.Round((r.Top-3)*sy);item.Key.SetBounds(x,y,contentInset+(int)Math.Round((r.Right-3)*sx)-x,contentInset+(int)Math.Round((r.Bottom-3)*sy)-y);Font original=originalFonts[item.Key];float points=Math.Max(8f,original.SizeInPoints*textScale);if(Math.Abs(item.Key.Font.SizeInPoints-points)>.05f){Font previous;scaledFonts.TryGetValue(item.Key,out previous);var font=new Font(original.FontFamily,points,original.Style,GraphicsUnit.Point);item.Key.Font=font;scaledFonts[item.Key]=font;if(previous!=null)previous.Dispose();}}
   for(int i=0;i<8;i++){names[i].GradientHeight=tags[i].GradientHeight=names[i].Height+tags[i].Height;tags[i].GradientOffset=names[i].Height;names[i].Invalidate();tags[i].Invalidate();}
   int w=ClientSize.Width,h=ClientSize.Height,e=edgeSize,c=e*2;
   Rectangle[] edges={new Rectangle(0,c,e,h-2*c),new Rectangle(w-e,c,e,h-2*c),new Rectangle(c,0,w-2*c,e),new Rectangle(0,0,c,c),new Rectangle(w-c,0,c,c),new Rectangle(c,h-e,w-2*c,e),new Rectangle(0,h-c,c,c),new Rectangle(w-c,h-c,c,c)};
   for(int i=0;i<8;i++){resizeEdges[i].Bounds=edges[i];if(i==3||i==4||i==6||i==7){var oldRegion=resizeEdges[i].Region;var corner=new Region(new Rectangle(0,(i==6||i==7)?c-e:0,c,e));corner.Union(new Rectangle((i==4||i==7)?c-e:0,0,e,c));resizeEdges[i].Region=corner;if(oldRegion!=null)oldRegion.Dispose();}resizeEdges[i].BringToFront();}
   UpdateRoundedCorners();Invalidate();foreach(var edge in resizeEdges)edge.Invalidate();
  }finally{ResumeLayout(false);}
 }
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);DrawWindowOutline(e.Graphics);}
 void DrawWindowOutline(Graphics graphics){
  float thickness=3f*Math.Max(1.5f,edgeSize*.375f),inset=thickness/2f;
  float w=ClientSize.Width-thickness,h=ClientSize.Height-thickness;
  float diameter=Math.Min(edgeSize*4-thickness,Math.Min(w,h));
  if(diameter<=0)return;
  var state=graphics.Save();
  graphics.SmoothingMode=SmoothingMode.HighQuality;
  graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
  using(var path=new GraphicsPath())using(var pen=new Pen(Color.FromArgb(150,150,150),thickness)){
   path.AddArc(inset,inset,diameter,diameter,180,90);
   path.AddArc(inset+w-diameter,inset,diameter,diameter,270,90);
   path.AddArc(inset+w-diameter,inset+h-diameter,diameter,diameter,0,90);
   path.AddArc(inset,inset+h-diameter,diameter,diameter,90,90);
   path.CloseFigure();graphics.DrawPath(pen,path);
  }
  graphics.Restore(state);
 }
 void UpdateRoundedCorners(){
  if(nativeRoundedCorners){var old=Region;Region=null;if(old!=null)old.Dispose();return;}
  int w=ClientSize.Width,h=ClientSize.Height;
  if(w<1||h<1)return;
  float diameter=Math.Min(edgeSize*4,Math.Min(w,h));
  using(var path=new GraphicsPath()){
   path.AddArc(0,0,diameter,diameter,180,90);
   path.AddArc(w-diameter,0,diameter,diameter,270,90);
   path.AddArc(w-diameter,h-diameter,diameter,diameter,0,90);
   path.AddArc(0,h-diameter,diameter,diameter,90,90);
   path.CloseFigure();
   var previous=Region;Region=new Region(path);if(previous!=null)previous.Dispose();
  }
 }
}
}
