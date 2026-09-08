using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace DovaInstaller;
public partial class ModsWindow:Window
{
 readonly string root;bool busy,activeSide=true;string? backup;ModFile[] active=[],inactive=[];Point dragOrigin;
 public ModsWindow(string root){this.root=root;InitializeComponent();Closing+=(_,e)=>e.Cancel=busy;Refresh();}
 void Refresh(){try{active=InstallCore.ScanMods(root).ToArray();inactive=OrganizerCore.ScanInactive(root).ToArray();Filter();}catch(Exception ex){Message.Text=ex.Message;}Buttons();}
 void Filter(){if(ModList==null||InactiveList==null)return;string text=SearchBox.Text;ModList.ItemsSource=active.Where(m=>m.Name.Contains(text,StringComparison.OrdinalIgnoreCase)).ToArray();InactiveList.ItemsSource=inactive.Where(m=>m.Name.Contains(text,StringComparison.OrdinalIgnoreCase)).ToArray();ActiveTitle.Text=$"ENABLED ({active.Length})";InactiveTitle.Text=$"DISABLED ({inactive.Length})";}
 void FilterChanged(object s,TextChangedEventArgs e)=>Filter();
 void Buttons(){if(RemoveButton==null)return;EnableButton.IsEnabled=!busy&&InactiveList.SelectedItems.Count>0;DisableButton.IsEnabled=!busy&&ModList.SelectedItems.Count>0;RemoveButton.IsEnabled=!busy&&(activeSide?ModList:InactiveList).SelectedItems.Count>0;}
 void SelectionChanged(object s,SelectionChangedEventArgs e){if(e.AddedItems.Count>0)activeSide=ReferenceEquals(s,ModList);Buttons();}
 void Working(bool value){busy=value;WorkProgress.Visibility=value?Visibility.Visible:Visibility.Collapsed;ModList.IsEnabled=InactiveList.IsEnabled=SearchBox.IsEnabled=ScanButton.IsEnabled=ImportButton.IsEnabled=DoneButton.IsEnabled=!value;Buttons();}
 async Task Move(bool enabled,ModFile[]? items=null){if(busy)return;items??=(enabled?InactiveList:ModList).SelectedItems.Cast<ModFile>().ToArray();if(items.Length==0)return;Working(true);Message.Text=enabled?"Enabling your mods...":"Parking those mods...";try{await Task.Run(()=>OrganizerCore.SetEnabled(root,items,enabled));Refresh();Message.Text=enabled?"Moved to enabled. Ready for your next modded launch.":"Moved to disabled. Still here when you want them.";}catch(Exception ex){Message.Text=ex.Message;}finally{Working(false);}}
 async void EnableMods(object s,RoutedEventArgs e)=>await Move(true);
 async void DisableMods(object s,RoutedEventArgs e)=>await Move(false);
 void Scan(object s,RoutedEventArgs e){if(!busy)Refresh();}
 void SelectAll(object s,RoutedEventArgs e){if(!busy)(activeSide?ModList:InactiveList).SelectAll();}
 void ClearSelection(object s,RoutedEventArgs e){if(!busy){ModList.UnselectAll();InactiveList.UnselectAll();}}
 void DragStart(object s,MouseButtonEventArgs e)=>dragOrigin=e.GetPosition(this);
 void DragMove(object s,MouseEventArgs e){if(busy||e.LeftButton!=MouseButtonState.Pressed)return;var p=e.GetPosition(this);if(Math.Abs(p.X-dragOrigin.X)<SystemParameters.MinimumHorizontalDragDistance&&Math.Abs(p.Y-dragOrigin.Y)<SystemParameters.MinimumVerticalDragDistance)return;var list=(ListBox)s;var items=list.SelectedItems.Cast<ModFile>().ToArray();if(items.Length==0)return;DragDrop.DoDragDrop(list,new DataObject("VariantMods",new ModDrag(ReferenceEquals(list,ModList),items)),DragDropEffects.Move);}
 sealed record ModDrag(bool Enabled,ModFile[] Files);
 void DragOverList(object s,DragEventArgs e){e.Effects=!busy&&e.Data.GetData("VariantMods") is ModDrag data&&data.Enabled!=ReferenceEquals(s,ModList)?DragDropEffects.Move:DragDropEffects.None;e.Handled=true;}
 async void DropList(object s,DragEventArgs e){e.Handled=true;if(!busy&&e.Data.GetData("VariantMods") is ModDrag data&&data.Enabled!=ReferenceEquals(s,ModList))await Move(ReferenceEquals(s,ModList),data.Files);}
 async void Import(object s,RoutedEventArgs e){if(busy)return;var d=new OpenFileDialog{Title="Import a mod",Filter="Mods (*.pak;*.zip)|*.pak;*.zip"};if(d.ShowDialog(this)!=true)return;if(MessageBox.Show(this,"Add these mods to enabled? Matching filenames will be replaced, with backups first.","Import mod",MessageBoxButton.OKCancel)!=MessageBoxResult.OK)return;Working(true);Message.Text="Importing your mods...";try{backup=await Task.Run(()=>OrganizerCore.Import(root,d.FileName));Refresh();Message.Text="Imported to enabled. New toys, same planet.";}catch(Exception ex){Message.Text=ex.Message;}finally{Working(false);}}
 async void Remove(object s,RoutedEventArgs e){if(busy)return;bool enabled=activeSide;var items=(enabled?ModList:InactiveList).SelectedItems.Cast<ModFile>().ToArray();if(items.Length==0)return;if(MessageBox.Show(this,"Remove these mods from the list? They'll go into a backup folder. To keep them handy, move them to Disabled instead.\n\n"+string.Join("\n",items.Select(m=>m.Name)),"Remove mods",MessageBoxButton.OKCancel)!=MessageBoxResult.OK)return;Working(true);Message.Text="Moving removed mods to a backup...";try{backup=await Task.Run(()=>enabled?InstallCore.RemoveMods(root,items).Backup:OrganizerCore.RemoveInactive(root,items));Refresh();Message.Text="Removed. Backups are there if you change your mind.";}catch(Exception ex){Message.Text=ex.Message;}finally{Working(false);}}
 void OpenBackup(object s,RoutedEventArgs e){try{string folder=backup??Path.Combine(InstallCore.Plan(root).Root,"DovaLocks-Backups");if(Directory.Exists(folder))Process.Start(new ProcessStartInfo(folder){UseShellExecute=true});else Message.Text="No backups yet. They appear after replacing or removing a mod.";}catch(Exception ex){Message.Text=ex.Message;}}
 void Done(object s,RoutedEventArgs e)=>Close();
}
