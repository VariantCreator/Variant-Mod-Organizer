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
 async void ChooseLog(object sender,RoutedEventArgs e)=>await ChooseReport(false);
 async void ChooseCrash(object sender,RoutedEventArgs e)=>await ChooseReport(true);
 async Task ChooseReport(bool crash)
 {
  var dialog=new OpenFileDialog{Title=crash?"Open an ICARUS crash report":"Open an ICARUS game log",InitialDirectory=DiagnosticPaths.StartFolder(crash),RestoreDirectory=true,CheckFileExists=true,Filter=crash?"Unreal crash reports|*.runtime-xml;*.xml;*.dmp|Unreal crash context|*.runtime-xml;*.xml|Minidumps|*.dmp":"Game logs|*.log"};
  if(dialog.ShowDialog(this)!=true)return;
  ChooseFile.IsEnabled=ChooseCrashFile.IsEnabled=false;ReadingProgress.Visibility=Visibility.Visible;Result.Text="Reading the report...";
  try {
   string chosen=dialog.FileName;
   var report=await Task.Run(()=>UnrealCrashReport.Read(chosen));
   if(!crash){selection++;timer.Stop();guard?.Dispose();guard=null;Collect.Content="Start collecting";source=chosen;SourceLabel.Text=source;collector=null;Role.IsEnabled=true;entries=0;Collect.IsEnabled=true;}
   new CrashReportWindow(report){Owner=this}.Show();ReportLabel.Text="Opened report: "+chosen;
   Result.Text=crash?"Crash report opened. Your game-log collection source hasn't changed. Nothing was uploaded.":"Game log opened. Start collecting to copy useful entries into DovaOutPut. Nothing was uploaded.";
  }
  catch(Exception ex){Result.Text="Couldn't open this report: "+ex.Message;}
  finally{ChooseFile.IsEnabled=ChooseCrashFile.IsEnabled=true;ReadingProgress.Visibility=Visibility.Collapsed;}
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
