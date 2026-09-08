using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DovaInstaller;
public partial class App : Application
{
 protected override async void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);
  if(e.Args.Length==3 && e.Args[0]=="--preview-crash")
  {
   var viewer=new CrashReportWindow(UnrealCrashReport.Read(e.Args[1]));MainWindow=viewer;var content=(FrameworkElement)viewer.Content;
   double width=viewer.Width-16,height=viewer.Height-40;content.Measure(new Size(width,height));content.Arrange(new Rect(0,0,width,height));content.UpdateLayout();
   var bitmap=new RenderTargetBitmap((int)width,(int)height,96,96,PixelFormats.Pbgra32);bitmap.Render(content);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(e.Args[2]))png.Save(stream);Shutdown();return;
  }
  if(e.Args.Length==4 && e.Args[0]=="--watch-output")
  {
   try{await DovaOutputLog.WatchGame(e.Args[2],e.Args[3],e.Args[1]);Shutdown();}
   catch{Shutdown(1);}return;
  }
  if(e.Args.Length==2 && e.Args[0]=="--open-lock-save")
  {
   var editor=new LockToolsWindow(); MainWindow=editor; editor.Show(); await editor.LoadSave(e.Args[1]); return;
  }
  if(e.Args.Length==2 && e.Args[0]=="--preview-diagnostics")
  {
   var diagnostic=new DiagnosticsWindow();MainWindow=diagnostic;var content=(FrameworkElement)diagnostic.Content;
   // Reserve the native window frame, rather than giving the content its space.
   double clientWidth=diagnostic.Width-16,clientHeight=diagnostic.Height-40;
   content.Measure(new Size(clientWidth,clientHeight));content.Arrange(new Rect(0,0,clientWidth,clientHeight));content.UpdateLayout();
   var bitmap=new RenderTargetBitmap((int)clientWidth,(int)clientHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(content);
   var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(e.Args[1]))png.Save(stream);Shutdown();return;
  }
  if(e.Args.Length==3 && e.Args[0]=="--preview-locks")
  {
   var editor=new LockToolsWindow(); MainWindow=editor; await editor.LoadSave(e.Args[1]);
   var content=(FrameworkElement)editor.Content;content.Measure(new Size(editor.Width,editor.Height));content.Arrange(new Rect(0,0,editor.Width,editor.Height));content.UpdateLayout();
   var bitmap=new RenderTargetBitmap((int)editor.Width,(int)editor.Height,96,96,PixelFormats.Pbgra32);bitmap.Render(content);
   var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(e.Args[2]))png.Save(stream);Shutdown();return;
  }
  if (e.Args.Length == 2 && e.Args[0] == "--verify-bundle")
  {
   using var payload = InstallCore.Payload();
   string sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(payload)).ToLowerInvariant();
   File.WriteAllText(e.Args[1], System.Text.Json.JsonSerializer.Serialize(new { release = InstallCore.Release, sha256 = sha, verified = sha == InstallCore.PayloadHash, installations = InstallCore.Detect() }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
   Shutdown(sha == InstallCore.PayloadHash ? 0 : 1); return;
  }
  if (e.Args.Length == 2 && e.Args[0] == "--preview-mods")
  {
   var root = InstallCore.Detect().First().Root;
   var manager = new ModsWindow(root); MainWindow = manager;
   var content = (FrameworkElement)manager.Content;
   content.Measure(new Size(manager.Width, manager.Height));
   content.Arrange(new Rect(0, 0, manager.Width, manager.Height)); content.UpdateLayout();
   var bitmap = new RenderTargetBitmap((int)manager.Width, (int)manager.Height, 96, 96, PixelFormats.Pbgra32);
   bitmap.Render(content); var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
   using(var stream = File.Create(e.Args[1])) png.Save(stream);
   Shutdown(); return;
  }
  var window = new MainWindow(); MainWindow = window;
  if (e.Args.Length == 2 && (e.Args[0] == "--preview" || e.Args[0] == "--preview-installed" || e.Args[0] == "--preview-online"))
  {
   window.InitializeLocations();
   if (e.Args[0] == "--preview-online") await window.CheckLatest();
   if (e.Args[0] == "--preview-installed") window.ShowInstalled(@"C:\Games\Icarus\Icarus\Content\Paks\Mods\Dova-Locks_P.pak");
   var content = (FrameworkElement)window.Content;
   content.Measure(new Size(window.Width, window.Height));
   content.Arrange(new Rect(0, 0, window.Width, window.Height)); content.UpdateLayout();
   var bitmap = new RenderTargetBitmap((int)window.Width, (int)window.Height, 96, 96, PixelFormats.Pbgra32);
   bitmap.Render(window.Content as Visual);
   var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
   using(var stream = File.Create(e.Args[1])) png.Save(stream);
   Shutdown(); return;
  }
  window.Show(); window.InitializeLocations(); _ = window.CheckLatest();
 }
}
