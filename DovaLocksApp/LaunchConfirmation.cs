using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace DovaInstaller;
public sealed class LaunchConfirmation:Window
{
 public LaunchConfirmation(Window owner,bool vanilla)
 {
  Owner=owner;Title="Ready to launch?";Width=450;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=new SolidColorBrush(Color.FromRgb(17,19,16));Foreground=Brushes.Beige;FontSize=14;
  var body=new StackPanel{Margin=new Thickness(26)};Content=body;body.Children.Add(new TextBlock{Text=vanilla?"Launch vanilla ICARUS?":"Launch modded ICARUS?",FontSize=24,Margin=new Thickness(0,0,0,12)});
  body.Children.Add(new TextBlock{Text=vanilla?"Steam will open ICARUS with your mods set aside. Your loadout will be kept for next time.":"Steam will open ICARUS with the mods on your Enabled list. Let's hope the wildlife behaves.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightGray});
  var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,24,0,0)};body.Children.Add(buttons);
  var cancel=new Button{Content="Cancel",IsCancel=true,Margin=new Thickness(0,0,10,0)};buttons.Children.Add(cancel);
  var launch=new Button{Content="Launch",IsDefault=true,Background=new SolidColorBrush(Color.FromRgb(231,204,147)),Foreground=Brushes.Black};launch.Click+=(_,_)=>DialogResult=true;buttons.Children.Add(launch);
 }
}
