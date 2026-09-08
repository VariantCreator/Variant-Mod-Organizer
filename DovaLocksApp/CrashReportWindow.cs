using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DovaInstaller;
public sealed class CrashReportWindow:Window
{
 public CrashReportWindow(UnrealCrashReport report)
 {
  Title="Unreal crash report";Width=820;Height=640;MinWidth=560;MinHeight=420;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=new SolidColorBrush(Color.FromRgb(17,19,16));Foreground=Brushes.Beige;
  var grid=new Grid{Margin=new Thickness(24)};Content=grid;grid.RowDefinitions.Add(new(){Height=GridLength.Auto});grid.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});grid.RowDefinitions.Add(new(){Height=GridLength.Auto});
  var title=new TextBlock{Text=System.IO.Path.GetFileName(report.FilePath),FontSize=22,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,14)};grid.Children.Add(title);
  var text=new TextBox{Text=report.Details,IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,FontSize=14,Padding=new Thickness(14),Background=new SolidColorBrush(Color.FromRgb(28,32,26)),Foreground=Brushes.Beige};Grid.SetRow(text,1);grid.Children.Add(text);
  var footer=new DockPanel{Margin=new Thickness(0,16,0,0)};Grid.SetRow(footer,2);grid.Children.Add(footer);
  var copy=new Button{Content="Copy report",HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(copy,Dock.Right);copy.Click+=(_,_)=>{try{Clipboard.SetText(report.Details);copy.Content="Copied";}catch{copy.Content="Couldn't copy. Try again";}};footer.Children.Add(copy);
  footer.Children.Add(new TextBlock{Text="Read locally. Nothing is uploaded.",VerticalAlignment=VerticalAlignment.Center});
 }
}
