using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AG4O3A {
 public partial class MainForm {
  bool checkingUpdates;
  bool updateDownloading;
  Form updateWindow;

  public async Task CheckUpdates(bool manual) {
   if(Program.Test || Program.Demo || checkingUpdates || preparingExit) return;
   if(updateWindow!=null && !updateWindow.IsDisposed) { updateWindow.Activate();return; }
   checkingUpdates=true;
   try {
    var release=await Task.Run((Func<UpdateRelease>)Updates.Check);
    if(IsDisposed || preparingExit || exitReady) return;
    if(release==null) {
     if(manual)MessageBox.Show(this,Lang.T("Já tem a versão mais recente."),"AG 4O3A");
     return;
    }
    ShowUpdate(release);
   } catch(Exception ex) {
    Storage.Log("Atualizações: "+ex.Message);
    if(manual && !IsDisposed)MessageBox.Show(this,Lang.T("Não foi possível verificar as atualizações.")+"\n"+ex.Message,"AG 4O3A");
   } finally { checkingUpdates=false; }
  }

  void ShowUpdate(UpdateRelease release) {
   var dialog=new Form();
   UI.Form(dialog,Lang.T("Atualização disponível"),new Size(440,205));
   dialog.StartPosition=FormStartPosition.CenterParent;
   dialog.MaximizeBox=false;
   var label=UI.Label(dialog,"AG 4O3A "+release.Version,20,20,400,32);
   label.Font=new Font("Segoe UI",14,FontStyle.Bold);
   var status=UI.Label(dialog,Lang.T("As suas definições serão mantidas."),20,62,400,60);
   var install=UI.Button(dialog,"Atualizar",145,145,130,34);
   var later=UI.Button(dialog,"Mais tarde",285,145,130,34);
   later.Click+=delegate{dialog.Close();};
   dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(updateDownloading)e.Cancel=true;};
   install.Click+=async delegate {
    if(updateDownloading || preparingExit) return;
    updateDownloading=true;install.Enabled=later.Enabled=false;
    status.Text=Lang.T("A descarregar e verificar a atualização...");
    try {
     string installer=await Task.Run(()=>Updates.Download(release));
     if(IsDisposed) return;
     Program.UpdateInstaller=installer;
     updateDownloading=false;
     dialog.Close();
     Close();
    } catch(Exception ex) {
     if(!dialog.IsDisposed) {
      status.Text=Lang.T("Não foi possível preparar a atualização.");
      MessageBox.Show(dialog,ex.Message,"AG 4O3A",MessageBoxButtons.OK,MessageBoxIcon.Warning);
     }
    } finally {
     updateDownloading=false;
     if(!dialog.IsDisposed)install.Enabled=later.Enabled=true;
    }
   };
   updateWindow=dialog;
   dialog.Show(this);
  }
 }
}

