using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
namespace DovaInstaller;
public sealed class LockToolsWindow:Window
{
 readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightGray,Margin=new Thickness(0,12,0,0)};
 readonly TextBlock world=new(){Text="Choose a world to see its locks",FontSize=18,TextWrapping=TextWrapping.Wrap};
 readonly ListBox locks=new(){Background=Brushes.Transparent,Foreground=Brushes.Beige,BorderThickness=new Thickness(0)};
 readonly StackPanel details=new(){Margin=new Thickness(20,0,0,0)};
 readonly ProgressBar progress=new(){Height=4,IsIndeterminate=true,Visibility=Visibility.Collapsed,Margin=new Thickness(0,12,0,0)};
 readonly List<Button> actions=[];
 readonly Grid workspace=new();
 readonly CheckBox showInactive=new(){Content="Show inactive locks",Foreground=Brushes.Beige,Margin=new Thickness(0,10,0,0)};
 Button apply=null!; string? save; EditableLocks? document; bool busy,dirty;
 static Brush Gold=>new SolidColorBrush(Color.FromRgb(231,204,147));
 public LockToolsWindow()
 {
  Title="World locks | Variant Mod Organizer";Width=940;Height=700;MinWidth=780;MinHeight=580;FontSize=14;FontFamily=new FontFamily("Segoe UI");WindowStartupLocation=WindowStartupLocation.CenterOwner;
  Background=new SolidColorBrush(Color.FromRgb(17,19,16));Foreground=Brushes.Beige;
  locks.Resources[SystemColors.HighlightBrushKey]=new SolidColorBrush(Color.FromRgb(57,59,45));
  locks.Resources[SystemColors.InactiveSelectionHighlightBrushKey]=new SolidColorBrush(Color.FromRgb(57,59,45));
  locks.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey]=Brushes.Beige;
  var shell=new DockPanel{Margin=new Thickness(28),Background=Background};Content=shell;
  var head=new StackPanel();DockPanel.SetDock(head,Dock.Top);shell.Children.Add(head);
  head.Children.Add(new TextBlock{Text="DOVA LOCKS / WORLD TOOLS",Foreground=Gold,FontSize=12});
  head.Children.Add(new TextBlock{Text="Your world's locks",FontSize=30,Margin=new Thickness(0,6,0,10)});
  head.Children.Add(new TextBlock{Text="Choose either A or B. Keep both files together so we can use the newest copy. For a hosted server, download the pair first.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightGray});
  var toolbar=new WrapPanel{Margin=new Thickness(0,16,0,18)};head.Children.Add(toolbar);
  toolbar.Children.Add(Action("Choose lock save",Choose));toolbar.Children.Add(Action("Edit JSON",EditJson));toolbar.Children.Add(Action("Export JSON",Export));toolbar.Children.Add(Action("Import JSON",Import));
  head.Children.Add(world);head.Children.Add(showInactive);showInactive.Checked+=(_,_)=>RefreshList();showInactive.Unchecked+=(_,_)=>RefreshList();
  var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);shell.Children.Add(footer);
  footer.Children.Add(progress);footer.Children.Add(status);
  apply=Action("Save changes to A and B",Apply);apply.HorizontalAlignment=HorizontalAlignment.Right;apply.Background=Gold;apply.Foreground=Brushes.Black;apply.Margin=new Thickness(0,16,0,0);apply.IsEnabled=false;footer.Children.Add(apply);
  status.Text="Nothing changes until you save. Original files are backed up first.";
  workspace.Margin=new Thickness(0,20,0,0);workspace.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(270)});workspace.ColumnDefinitions.Add(new ColumnDefinition());shell.Children.Add(workspace);
  workspace.Children.Add(new Border{Child=locks,Padding=new Thickness(10),BorderThickness=new Thickness(1),BorderBrush=new SolidColorBrush(Color.FromRgb(57,59,51))});
  var scroll=new ScrollViewer{Content=details,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetColumn(scroll,1);workspace.Children.Add(scroll);
  locks.SelectionChanged+=(_,_)=>ShowLock();
  Closing+=(_,e)=>{if(busy)e.Cancel=true;else if(dirty&&MessageBox.Show(this,"Discard your unsaved lock changes?","Unsaved changes",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)e.Cancel=true;};
 }
 Button Action(string label,RoutedEventHandler handler){var b=new Button{Content=label,Padding=new Thickness(14,9,14,9),Margin=new Thickness(0,0,8,0)};b.Click+=handler;actions.Add(b);return b;}
 void Working(bool value){busy=value;progress.Visibility=value?Visibility.Visible:Visibility.Collapsed;workspace.IsEnabled=!value;showInactive.IsEnabled=!value;foreach(var b in actions)b.IsEnabled=!value;apply.IsEnabled=!value&&dirty;}
 bool Discard()=>!dirty||MessageBox.Show(this,"Discard your unsaved changes and open another copy?","Unsaved changes",MessageBoxButton.YesNo)==MessageBoxResult.Yes;
 async void Choose(object s,RoutedEventArgs e){if(!Discard())return;var d=new OpenFileDialog{Filter="Dova Locks saves|DovaLocks_*.sav",InitialDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Icarus","Saved","SaveGames")};if(d.ShowDialog(this)==true)await LoadSave(d.FileName);}
 public async Task LoadSave(string path)
 {
  Working(true);status.Text="Reading your world locks...";
  try {var loaded=await Task.Run(()=>LockSaveEditor.Export(path));save=path;document=loaded;dirty=false;world.Text=loaded.World+"  /  "+loaded.Locks.Count+" lock records";RefreshList();status.Text="Loaded the newest copy. Select a lock to manage access. Stop the game or server before saving.";}
  catch(Exception ex){status.Text="Couldn't open this save: "+ex.Message;}
  finally{Working(false);}
 }
 void RefreshList(){string? selected=(locks.SelectedItem as ListBoxItem)?.Tag as string;locks.Items.Clear();details.Children.Clear();if(document==null)return;var visible=LockSaveEditor.Filter(document,showInactive.IsChecked==true);world.Text=document.World+" / "+document.Locks.Count(LockSaveEditor.IsActive)+" active locks";foreach(var l in visible.Locks)locks.Items.Add(new ListBoxItem{Tag=l.Id,Content=(l.RemoveLock?"[Clear on save] ":!LockSaveEditor.IsActive(l)?"[Inactive] ":"")+(string.IsNullOrEmpty(l.Owner.Name)?"Unnamed owner":l.Owner.Name)+"\n"+l.Id,Padding=new Thickness(8),ToolTip=l.Id});if(locks.Items.Count>0)locks.SelectedItem=locks.Items.Cast<ListBoxItem>().FirstOrDefault(i=>(string)i.Tag==selected)??locks.Items[0];else details.Children.Add(new TextBlock{Text="No active locks in this save. Enable Show inactive locks to see older records.",TextWrapping=TextWrapping.Wrap});}
 void Changed(){dirty=true;apply.IsEnabled=true;status.Text="Changes are pending. Save when you're ready.";}
 void ShowLock()
 {
  details.Children.Clear();if(document==null||locks.SelectedIndex<0)return;int index=document.Locks.FindIndex(l=>l.Id==(string)((ListBoxItem)locks.SelectedItem).Tag);if(index<0)return;var l=document.Locks[index];
  details.Children.Add(new TextBlock{Text=string.IsNullOrEmpty(l.Owner.Name)?"Unnamed owner":l.Owner.Name,FontSize=23,Foreground=Gold});
  details.Children.Add(new TextBlock{Text="Owner Steam ID: "+l.Owner.SteamId,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,16)});
  details.Children.Add(new TextBlock{Text=l.Pin==""?"This record is unlocked.":"PIN is set. It stays hidden here.",Foreground=Brushes.LightGray});
  var clear=new Button{Content=l.RemoveLock?"Keep this lock":"Clear this lock",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,12,0,20)};
  clear.Click+=(_,_)=>{document.Locks[index]=l with{RemoveLock=!l.RemoveLock};Changed();RefreshList();};details.Children.Add(clear);
  details.Children.Add(new TextBlock{Text="Player access",FontSize=18,Margin=new Thickness(0,0,0,12)});
  if(l.Players.Count==0)details.Children.Add(new TextBlock{Text="No other players remembered for this lock.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightGray});
  for(int n=0;n<l.Players.Count;n++){int personIndex=n;var person=l.Players[n];var row=new StackPanel{Margin=new Thickness(0,0,0,16)};row.Children.Add(new TextBlock{Text=(string.IsNullOrEmpty(person.Name)?person.SteamId:person.Name)+" / "+(person.Blocked?"Blocked":person.Role),TextWrapping=TextWrapping.Wrap});row.Children.Add(new TextBlock{Text=person.SteamId,FontSize=12,Foreground=Brushes.LightGray});var button=new Button{Content=person.Blocked?"Restore access":"Block access",HorizontalAlignment=HorizontalAlignment.Left,Padding=new Thickness(12,6,12,6),Margin=new Thickness(0,6,0,0)};button.Click+=(_,_)=>{l.Players[personIndex]=person with{Blocked=!person.Blocked};Changed();ShowLock();};row.Children.Add(button);details.Children.Add(row);}
 }
 void Export(object s,RoutedEventArgs e){if(document==null){status.Text="Choose a lock save first.";return;}var d=new SaveFileDialog{Filter="Editable lock file|*.json",FileName="Dova-Locks-editable.json"};if(d.ShowDialog(this)!=true)return;try{File.WriteAllText(d.FileName,JsonSerializer.Serialize(LockSaveEditor.Filter(document,showInactive.IsChecked==true),LockSaveEditor.JsonOptions));status.Text="Exported. The editable copy contains PINs and Steam IDs, so keep it private.";}catch(Exception ex){status.Text=ex.Message;}}
 void Import(object s,RoutedEventArgs e){if(save==null){status.Text="Choose a lock save first.";return;}if(!Discard())return;var d=new OpenFileDialog{Filter="Editable lock file|*.json"};if(d.ShowDialog(this)!=true)return;try{var edited=JsonSerializer.Deserialize<EditableLocks>(File.ReadAllText(d.FileName),LockSaveEditor.JsonOptions)??throw new IOException("Empty edit file.");LockSaveEditor.ApplyEdits(LockSaveEditor.Latest(save),edited);document=LockSaveEditor.MergeView(LockSaveEditor.Export(save),edited);Changed();RefreshList();}catch(Exception ex){status.Text=ex.Message;}}
 void EditJson(object sender,RoutedEventArgs e)
 {
  if(save==null||document==null){status.Text="Choose a lock save first.";return;}
  var editor=new LockJsonWindow(document,async edited=>{
   try { LockSaveEditor.ApplyEdits(LockSaveEditor.Latest(save),edited);document=LockSaveEditor.MergeView(document!,edited);Changed();RefreshList();return await SaveEdits(false); }
   catch(Exception ex){status.Text=ex.Message;return false;}
  },showInactive.IsChecked==true);editor.Owner=this;editor.ShowDialog();
 }
 async void Apply(object s,RoutedEventArgs e)=>await SaveEdits(true);
 async Task<bool> SaveEdits(bool confirm)
 {
  if(save==null||document==null||!dirty)return false;
  if(confirm&&MessageBox.Show(this,"Save these changes to both A and B? Stop the game or server first. Backups will be created before writing.","Save world locks",MessageBoxButton.OKCancel,MessageBoxImage.Question)!=MessageBoxResult.OK)return false;
  Working(true);status.Text="Backing up and saving both copies...";
  try {foreach(var game in InstallCore.Detect())InstallCore.EnsureGameClosed(game.Root);var path=save;var edited=document;string backup=await Task.Run(()=>LockSaveEditor.ImportDocument(path,edited));dirty=false;await LoadSave(path);status.Text="Both saves updated. Upload both to your hosted server. Backups: "+backup;return true;}
  catch(Exception ex){status.Text="Nothing confirmed saved: "+ex.Message;return false;}
  finally{Working(false);}
 }
}
