#r "System.IO"
#r "System.Drawing"
#r "System.Windows.Forms"
#r "Microsoft.Win32"
using System.Windows.Forms;

Console.WriteLine("Execute file: befor-make-current.csx");

string appName = "Corney";

string mainDirPath = System.IO.Path.Combine(Syrup.CurrentAppPath, "main");
string corenyDir = System.IO.Path.Combine(mainDirPath, "Corney");
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