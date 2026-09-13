using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace DovaInstaller;

public sealed class LockJsonWindow : Window
{
 readonly TextBox text = new() { AcceptsReturn=true, AcceptsTab=true, IsReadOnly=true, FontFamily=new FontFamily("Consolas"), FontSize=14, VerticalScrollBarVisibility=ScrollBarVisibility.Auto, HorizontalScrollBarVisibility=ScrollBarVisibility.Auto, Background=new SolidColorBrush(Color.FromRgb(22,29,24)), Foreground=Brushes.Beige, Padding=new Thickness(12) };
 bool busy, editing;
 public LockJsonWindow(EditableLocks document, Func<EditableLocks,Task<bool>> apply, bool includeInactive=false)
 {
  Title="Edit world locks | Variant Mod Organizer";Width=920;Height=740;MinWidth=650;MinHeight=480;WindowStartupLocation=WindowStartupLocation.CenterOwner;
  Background=new SolidColorBrush(Color.FromRgb(17,19,16));Foreground=Brushes.Beige;
  var panel=new DockPanel {Margin=new Thickness(24)};Content=panel;
  var header=new TextBlock {Text="Your lock save, as JSON",FontSize=25,Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(header,Dock.Top);panel.Children.Add(header);
  var hint=new TextBlock {Text="Click Edit to make changes. Apply validates the JSON, backs up your saves, and writes both A and B. Stop the game or server first.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,16)};DockPanel.SetDock(hint,Dock.Top);panel.Children.Add(hint);
  var filter=new CheckBox{Content="Show inactive locks",Foreground=Brushes.Beige,IsChecked=includeInactive,Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(filter,Dock.Top);panel.Children.Add(filter);
  var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);
  var status=new TextBlock {TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,10)};footer.Children.Add(status);
  var progress=new ProgressBar {IsIndeterminate=true,Height=4,Visibility=Visibility.Collapsed};footer.Children.Add(progress);
  var buttons=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,12,0,0)};footer.Children.Add(buttons);
  var edit=new Button {Content="Edit",Padding=new Thickness(20,9,20,9),Margin=new Thickness(0,0,10,0)};
  var save=new Button {Content="Apply",IsEnabled=false,Padding=new Thickness(20,9,20,9)};buttons.Children.Add(edit);buttons.Children.Add(save);
  void Refresh()=>text.Text=JsonSerializer.Serialize(LockSaveEditor.Filter(document,filter.IsChecked==true),LockSaveEditor.JsonOptions);Refresh();filter.Checked+=(_,_)=>Refresh();filter.Unchecked+=(_,_)=>Refresh();panel.Children.Add(text);
  edit.Click+=(_,_)=>{editing=true;filter.IsEnabled=false;text.IsReadOnly=false;edit.IsEnabled=false;save.IsEnabled=true;text.Focus();};
  save.Click+=async(_,_)=>{
   try {
    var edited=JsonSerializer.Deserialize<EditableLocks>(text.Text,LockSaveEditor.JsonOptions)??throw new JsonException("The JSON is empty.");
    busy=true;text.IsReadOnly=true;save.IsEnabled=false;progress.Visibility=Visibility.Visible;status.Text="Validating and saving...";
    if(await apply(edited)){editing=false;status.Text="Saved to both A and B. For a hosted server, upload both edited saves.";busy=false;DialogResult=true;}
    else {status.Text="Not saved. Check the save-tools window for the error, then correct the JSON or reopen a fresh save.";text.IsReadOnly=false;save.IsEnabled=true;}
   } catch(JsonException ex){status.Text=$"Invalid JSON at line {(ex.LineNumber??0)+1}: {ex.Message}";}
   catch(Exception ex){status.Text="Not saved: "+ex.Message;text.IsReadOnly=false;save.IsEnabled=true;}
   finally {busy=false;progress.Visibility=Visibility.Collapsed;}
  };
  Closing+=(_,e)=>{if(busy)e.Cancel=true;else if(editing&&MessageBox.Show(this,"Close without applying your JSON edits?","Unsaved edits",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)e.Cancel=true;};
 }
}
