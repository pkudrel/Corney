#r "System.IO"
#r "System.Drawing"
#r "System.Windows.Forms"
using System.Windows.Forms;
using Microsoft.Win32;

Console.WriteLine("Execute file: befor-make-current.csx");

string appName = "Corney";
string appDirPath = System.IO.Directory.GetParent(Syrup.CurrentAppPath).FullName;
string mainAppDir = System.IO.Path.Combine(appDirPath, appName);

string mainDirPath = System.IO.Path.Combine(mainAppDir, "main");

string corenyDir = System.IO.Path.Combine(mainDirPath, appName);
string corenyExe = System.IO.Path.Combine(corenyDir, "Corney.exe");

string app = $"\"{corenyExe}\" -hide";

var path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

try
{
    rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
    rk.SetValue(appName, app);
}
catch (Exception)
{
    return false;
}