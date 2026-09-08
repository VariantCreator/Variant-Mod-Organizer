using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DovaInstaller;
public partial class DiagnosticsWindow : Window
{
 string source=DovaOutputLog.DefaultSource;
 DovaOutputLog? collector;
 FileStream? guard;
 readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromSeconds(2)};
 bool reading;
 int entries;
 int selection;
 public DiagnosticsWindow()
 {
  InitializeComponent();SourceLabel.Text=source;
  timer.Tick+=async(_,_)=>await Read();Closed+=(_,_)=>{timer.Stop();guard?.Dispose();};
 }
 async void ChooseLog(object sender,RoutedEventArgs e)
 {
  var dialog=new OpenFileDialog{Title="Open an Unreal crash report or game log",Filter="Crash reports and logs|*.runtime-xml;*.xml;*.dmp;*.log;*.txt|Unreal crash context|*.runtime-xml;*.xml|Minidumps|*.dmp|Logs|*.log;*.txt|All files|*.*"};
  if(dialog.ShowDialog(this)!=true)return;selection++;timer.Stop();guard?.Dispose();guard=null;Collect.Content="Start collecting";source=dialog.FileName;SourceLabel.Text=source;collector=null;Role.IsEnabled=true;entries=0;
  ChooseFile.IsEnabled=false;Collect.IsEnabled=false;Result.Text="Reading the report...";
  try{var report=await Task.Run(()=>UnrealCrashReport.Read(source));new CrashReportWindow(report){Owner=this}.Show();Result.Text="Report opened in a separate window. Nothing was uploaded.";Collect.IsEnabled=Path.GetExtension(source).Equals(".log",StringComparison.OrdinalIgnoreCase);}
  catch(Exception ex){Result.Text="Couldn't open this report: "+ex.Message;}
  finally{ChooseFile.IsEnabled=true;}
 }
 async void Toggle(object sender,RoutedEventArgs e)
 {
  if(timer.IsEnabled){timer.Stop();guard?.Dispose();guard=null;Collect.Content="Start collecting";return;}
  try{Directory.CreateDirectory(DovaOutputLog.DefaultFolder);guard=new FileStream(Path.Combine(DovaOutputLog.DefaultFolder,Role.SelectedIndex==0?"Client.collector.lock":"Server.collector.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
  catch(IOException){Result.Text="The background collector is already running. Open the DovaOutPut folder to see its log.";return;}
  catch(Exception ex){Result.Text="Couldn't start collecting: "+ex.Message;return;}
  collector??=new DovaOutputLog(source,DovaOutputLog.DefaultFolder,Role.SelectedIndex==0?"Client":"Server");
  Role.IsEnabled=false;timer.Start();Collect.Content="Stop collecting";await Read();
 }
 async Task Read()
 {
  if(reading||collector==null)return;reading=true;int current=selection;
  try {int added=await Task.Run(collector.Poll);if(current!=selection)return;entries+=added;Result.Text=File.Exists(source)?$"Collected {entries} entries. Waiting for more. Keep the original crash folder too if ICARUS crashed.":"Waiting for Icarus.log. Start ICARUS or choose the log file manually.";}
  catch(Exception ex){if(current!=selection)return;timer.Stop();guard?.Dispose();guard=null;Collect.Content="Start collecting";Result.Text="Couldn't read this log: "+ex.Message;}
  finally{reading=false;}
 }
 void OpenOutput(object sender,RoutedEventArgs e)
 {
  try{Directory.CreateDirectory(DovaOutputLog.DefaultFolder);Process.Start(new ProcessStartInfo(DovaOutputLog.DefaultFolder){UseShellExecute=true});}
  catch(Exception ex){Result.Text=ex.Message;}
 }
}
