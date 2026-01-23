
using Microsoft.VisualBasic.FileIO;

FileBrowser.Browser myBrowser = new FileBrowser.Browser();

Console.WriteLine("Start");

string FilePath = myBrowser.GetFolderPath();

Console.WriteLine(FilePath);