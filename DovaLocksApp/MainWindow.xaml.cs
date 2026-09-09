using System.Diagnostics;

using System.IO;

using System.Windows;

using System.Windows.Controls;

using Microsoft.Win32;

namespace DovaInstaller;

public partial class MainWindow : Window

{

 void UpdateOrganizer(object sender,RoutedEventArgs e){if(!busy)new OrganizerUpdateWindow{Owner=this}.ShowDialog();}
 void OpenWebsite(object sender,RoutedEventArgs e)
 {
  try { Process.Start(new ProcessStartInfo("https://variantinteractivemap.org"){UseShellExecute=true}); }
  catch(Exception ex) { Status.Text=ex.Message; }
 }
 bool busy;

 readonly ReleaseClient client = new();

 ModRelease? latest;

 GameLocation? Selected => Locations.SelectedItem as GameLocation;

 public MainWindow() { InitializeComponent(); Closing += (_,e) => e.Cancel = busy; }

 public void InitializeLocations()

 {

  try {

   var locations=InstallCore.Detect().ToList(); var saved=AppPreferences.Load();

   var valid = saved == null ? null : InstallCore.NormalizeGame(saved);

   if(valid!=null && locations.All(x=>!x.Root.Equals(valid,StringComparison.OrdinalIgnoreCase)))locations.Add(new(valid,"Saved ICARUS folder"));

   Locations.ItemsSource=locations; Locations.Visibility=locations.Count>1?Visibility.Visible:Visibility.Collapsed;

   Locations.SelectedItem=locations.FirstOrDefault(x=>x.Root.Equals(valid,StringComparison.OrdinalIgnoreCase)) ?? (locations.Count==1?locations[0]:null); RefreshPlan();

  } catch { Status.Text="Couldn't scan Steam libraries. Use Change folder to choose ICARUS."; RefreshPlan(); }

 }

 void RefreshPlan()

 {

  VersionLabel.Text=latest==null?"Included: "+InstallCore.Release:"Latest: "+latest.Version;

  ManageButton.IsEnabled=OpenFolderButton.IsEnabled=ModdedButton.IsEnabled=VanillaButton.IsEnabled=Selected!=null&&!busy; SaveToolsButton.IsEnabled=!busy;

  InstallButton.IsEnabled=Selected!=null&&!busy; InstallButton.Content="INSTALL / UPDATE";

  if(Selected==null) { GamePath.Text="Choose your ICARUS installation with Change folder."; InstalledLabel.Text="No game selected"; PlanText.Text="Select the game you use to join the server."; return; }

  GamePath.Text=Selected.Root;

  try {

   ModeLabel.Text=OrganizerCore.IsVanilla(Selected.Root)?"Vanilla mode: your mods are stored safely. Modded brings them back.":"Modded mode: your installed mods are ready. Vanilla keeps them aside."; var plan=InstallCore.Plan(Selected.Root); var file=Path.Combine(plan.Mods,InstallCore.FileName);

   var hash=File.Exists(file)?InstallCore.Hash(file):null;

   bool current=hash!=null&&hash==(latest?.Hash??InstallCore.PayloadHash);

   InstalledLabel.Text=current?(latest==null?"Included version is installed":"You're up to date. Version "+latest.Version):(hash==null?"Dova Locks is not installed here":"Dova Locks is installed");

   if(current) InstallButton.Content="REINSTALL";

   PlanText.Text=latest==null?"Install / Update checks the latest published version before changing files.":current?"Ready for a server running this version.":"Install the latest release from Dova. Your existing locks and access are kept.";

  } catch(Exception ex) {InstallButton.IsEnabled=false;PlanText.Text=ex.Message;}

 }

 void SetBusy(bool value) {busy=value;OrganizerUpdateButton.IsEnabled=!value;CheckButton.IsEnabled=BrowseButton.IsEnabled=Locations.IsEnabled=!value;Progress.Visibility=value?Visibility.Visible:Visibility.Collapsed;RefreshPlan();}

 public async Task CheckLatest()

 {

  if(busy)return;SetBusy(true);StatusTitle.Text="CHECKING FOR UPDATES";

  try {latest=await client.Latest();Notes.Text=latest.Notes;StatusTitle.Text="LATEST RELEASE READY";Status.Text="Choose Install / Update when your server is using this version.";}

  catch {latest=null;Notes.Text="Release notes are unavailable offline.";StatusTitle.Text="COULDN'T CHECK FOR UPDATES";Status.Text="Check your internet connection and try again. Installation needs a successful version check before it can continue.";}

  finally {SetBusy(false);}

 }

 async void CheckUpdates(object sender,RoutedEventArgs e)=>await CheckLatest();

 void SelectionChangedLocation(object sender,SelectionChangedEventArgs e) {if(!IsInitialized)return; if(Selected!=null)try{AppPreferences.Save(Selected.Root);}catch{} RefreshPlan();}

 void Browse(object sender,RoutedEventArgs e)

 {

  var dialog=new OpenFolderDialog{Title="Choose your ICARUS game or server folder"};if(dialog.ShowDialog(this)!=true)return;

  var root=InstallCore.NormalizeGame(dialog.FolderName);if(root==null){Status.Text="Choose the ICARUS folder containing Content and Binaries, or its parent folder.";return;}

  Locations.ItemsSource=new[]{new GameLocation(root,"ICARUS")};Locations.SelectedIndex=0;Locations.Visibility=Visibility.Collapsed;

 }

 async void Install(object sender,RoutedEventArgs e)

 {

  if(busy||Selected==null)return;var root=Selected.Root;

  SetBusy(true);StatusTitle.Text="INSTALLING DOVA LOCKS";

  try {

   InstallCore.EnsureGameClosed(root);

   var progress=new Progress<string>(message=>Status.Text=message);

   Status.Text="Checking and downloading the latest published version...";

   var download=await client.CurrentDownload();latest=download.Release;Notes.Text=latest.Notes;

   OrganizerCore.SetMode(root,false);

   var result=await Task.Run(()=>InstallCore.Apply(root,()=>new MemoryStream(download.Bytes),download.Release.Hash,InstallCore.EnsureGameClosed,progress,release:download.Release.Version));

   ShowInstalled(result.Destination);

  } catch(UnauthorizedAccessException){StatusTitle.Text="FOLDER ACCESS NEEDED";Status.Text="Windows blocked this game folder. Close the app and open Variant Mod Organizer with Run as administrator, then retry.";}

  catch(Exception ex){StatusTitle.Text="UPDATE STOPPED";Status.Text=ex.Message;}

  finally{SetBusy(false);}

 }

 internal void ShowInstalled(string destination){StatusTitle.Text="READY TO PLAY";Status.Text="Dova Locks is installed. Launch ICARUS and join your server.";RefreshPlan();}

 void ManageMods(object sender,RoutedEventArgs e){if(busy||Selected==null)return;try { OrganizerCore.SetMode(Selected.Root,false); new ModsWindow(Selected.Root){Owner=this}.ShowDialog(); } catch(Exception ex){Status.Text=ex.Message;} RefreshPlan();}

 async void LaunchModded(object sender,RoutedEventArgs e)=>await Launch(false);

 async void LaunchVanilla(object sender,RoutedEventArgs e)=>await Launch(true);

 async Task Launch(bool vanilla)

 {

  if(busy||Selected==null)return;if(new LaunchConfirmation(this,vanilla).ShowDialog()!=true)return;string root=Selected.Root;SetBusy(true);

  try {var launch=OrganizerCore.SteamLaunchTarget(root); await Task.Run(()=>OrganizerCore.SetMode(root,vanilla));Process.Start(new ProcessStartInfo(launch){UseShellExecute=true});
   // A separate quiet worker keeps collecting if the organizer window is closed.
   try{var worker=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};foreach(var arg in new[]{"--watch-output","Client",DovaOutputLog.DefaultSource,DovaOutputLog.DefaultFolder})worker.ArgumentList.Add(arg);Process.Start(worker);}catch{}
   StatusTitle.Text="LAUNCHING ICARUS";Status.Text=vanilla?"Your mods are kept aside until you switch back to modded.":"ICARUS is starting with your installed mods.";}

  catch(Exception ex){StatusTitle.Text="COULDN'T LAUNCH";Status.Text=ex.Message;}

  finally{SetBusy(false);}

 }

 void SaveTools(object sender,RoutedEventArgs e){new LockToolsWindow(){Owner=this}.ShowDialog();}
 void Diagnostics(object sender,RoutedEventArgs e){new DiagnosticsWindow(){Owner=this}.Show();}

 void OpenFolder(object sender,RoutedEventArgs e){if(Selected==null)return;try{var p=InstallCore.Plan(Selected.Root).Mods;if(Directory.Exists(p))Process.Start(new ProcessStartInfo(p){UseShellExecute=true});else Status.Text="The mods folder will be created when you install the mod.";}catch(Exception ex){Status.Text=ex.Message;}}

}

