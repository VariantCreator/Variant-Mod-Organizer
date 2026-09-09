using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace DovaInstaller;
public sealed class OrganizerUpdateWindow:Window
{
 readonly OrganizerUpdate client=new();readonly CancellationTokenSource cancel=new();
 readonly TextBlock status=new(){Text="Checking GitHub...",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,12)};
 readonly TextBlock notes=new(){TextWrapping=TextWrapping.Wrap};
 readonly ProgressBar progress=new(){Height=6,IsIndeterminate=true,Margin=new Thickness(0,12,0,12)};
 readonly Button install=new(){Content="Download and update",IsEnabled=false,Margin=new Thickness(0,12,0,0),HorizontalAlignment=HorizontalAlignment.Right};
 OrganizerRelease? release;bool downloading;
 public OrganizerUpdateWindow()
 {
  Title="Update Variant Mod Organizer";Width=620;Height=480;MinWidth=520;MinHeight=380;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=(Brush)new BrushConverter().ConvertFromString("#111310")!;Foreground=Brushes.Beige;
  var layout=new DockPanel{Margin=new Thickness(26)};Content=layout;
  var heading=new TextBlock{Text=$"Variant Mod Organizer {OrganizerUpdate.CurrentVersion.ToString(3)}",FontSize=24};DockPanel.SetDock(heading,Dock.Top);layout.Children.Add(heading);
  DockPanel.SetDock(status,Dock.Top);layout.Children.Add(status);DockPanel.SetDock(progress,Dock.Top);layout.Children.Add(progress);
  var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);footer.Children.Add(new TextBlock{Text="The organizer will close to apply the update. Your mods and settings stay where they are.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,16,0,0)});footer.Children.Add(install);layout.Children.Add(footer);
  layout.Children.Add(new ScrollViewer{Content=notes,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
  install.Click+=Install;Loaded+=async(_,_)=>await Check();Closed+=(_,_)=>cancel.Cancel();
 }
 async Task Check()
 {
  try{release=await client.Latest(cancel.Token);notes.Text=release.Notes;bool newer=OrganizerUpdate.IsNewer(release.Version,OrganizerUpdate.CurrentVersion);status.Text=newer?$"Version {release.Version.ToString(3)} is ready.":"You're up to date. No update needed.";install.IsEnabled=newer;}
  catch(OperationCanceledException){}
  catch(Exception ex){status.Text="Couldn't check for organizer updates: "+ex.Message;}
  finally{progress.Visibility=Visibility.Collapsed;}
 }
 async void Install(object sender,RoutedEventArgs e)
 {
  if(release==null||downloading)return;downloading=true;install.IsEnabled=false;progress.Visibility=Visibility.Visible;progress.IsIndeterminate=false;status.Text="Downloading and verifying the organizer update...";
  try{
   string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VariantModOrganizer","Updates");
   string installer=await client.Download(release,folder,new Progress<double>(n=>progress.Value=n),cancel.Token);cancel.Token.ThrowIfCancellationRequested();
   string installed=Path.GetDirectoryName(Environment.ProcessPath)??throw new IOException("Couldn't find the installed organizer.");
   _ = Process.Start(OrganizerUpdate.InstallerCommand(installer,installed))??throw new IOException("Windows couldn't start the update.");
   Application.Current.Shutdown();
  }catch(OperationCanceledException){}
  catch(Exception ex){status.Text="Update not applied: "+ex.Message;install.IsEnabled=true;}
  finally{downloading=false;progress.Visibility=Visibility.Collapsed;}
 }
}
