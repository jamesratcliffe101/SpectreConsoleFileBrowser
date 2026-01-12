using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

// make attributes priate instead of all being public
// Can't select drive on linux currently

namespace FileBrowser
{
    internal static class Icon
    {
        public const string upArrow = ":upwards_button:";
        public const string ok = ":ok_button:";
        public const string plus = ":plus:";
        public const string disk = ":computer_disk:";
    }
    
    public class Browser
    {
        public bool CanDisplayIcons { get; set; } = true;
        private bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private string[]? Drives { get; set; }

        private string record = "";

        public int PageSize { get; set; } = 15;
        public bool CanCreateFolder { get; set; } = true;
        public string ActualFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public string LevelUpText { get; set; } = "Go to upper level";
        public string ActualFolderText { get; set; } = "Selected Folder";
        public string MoreChoicesText { get; set; } = "Use arrows Up and Down to select";
        public string CreateNewText { get; set; } = "Create new folder";
        public string SelectFileText { get; set; } = "Select File";
        public string SelectFolderText { get; set; } = "Select Folder";
        public string SelectDriveText { get; set; } = "Select Drive";
        public string SelectActualText { get; set; } = "Select Actual Folder";

        private Dictionary<string, Action> SelectionList = new ();

        public Browser()
        {
            
        }

        /// <summary>
        /// Draws the file browser content to the screen using SpectreConsole
        /// </summary>
        private void DisplayFileBrower()
        {
            
        }

        private string FormatEntry(string icon, string text)
        {
            return $"{(CanDisplayIcons ? icon : "")}[green]{text}[/]";
        }

        /// <summary>
        /// Asks for folder name and creates a new folder
        /// </summary>
        private void createNewFolder()
        {
            string folderName = AnsiConsole.Ask<string>("[blue]" + CreateNewText + ": [/]");
            if (folderName != null)
            {
                try
                {
                    Directory.CreateDirectory(folderName);
                    record = Path.Combine(ActualFolder, folderName);
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteLine("[red]Error: [/]" + ex.Message);
                }
            }
        }

        private string GetPath(string startingPath, bool SelectFile)
        {
            string lastFolder = ActualFolder;
            while (true)
            {
                string headerText = SelectFile ? SelectFileText : SelectFolderText;
                string[] directoriesList;
                Directory.SetCurrentDirectory(ActualFolder);

                DirectoryInfo? ParentDir = new DirectoryInfo(ActualFolder).Parent;

                AnsiConsole.Clear();
                AnsiConsole.WriteLine();
                var rule = new Rule($"[b][green]{headerText}[/][/]").Centered();
                AnsiConsole.Write(rule);

                AnsiConsole.WriteLine();
                AnsiConsole.Markup($"[b][Yellow]{ActualFolderText}: [/][/]");
                var DirectoryPath = new TextPath(ActualFolder.ToString());
                DirectoryPath.RootStyle = new Style(foreground: Color.Green);
                DirectoryPath.SeparatorStyle = new Style(foreground: Color.Green);
                DirectoryPath.StemStyle = new Style(foreground: Color.Blue);
                DirectoryPath.LeafStyle = new Style(foreground: Color.Yellow);
                AnsiConsole.Write(DirectoryPath);
                AnsiConsole.WriteLine();

                Dictionary<string, string> folders = new Dictionary<string, string>();
                // get list of drives
                // key = the file/folder name
                // element = path to file/folder

                // get content in directory
                directoriesList = Directory.GetDirectories(Directory.GetCurrentDirectory());
                lastFolder = ActualFolder;

                if (IsWindows)
                {
                    folders.Add(FormatEntry(":computer_disk:", SelectDriveText), "/////");
                }

                if (ParentDir is not null)
                {
                    folders.Add(FormatEntry(":upwards_button: ", LevelUpText), ParentDir.FullName);
                }

                if (!SelectFile)
                {
                    folders.Add(FormatEntry(":ok_button: ", SelectActualText), Directory.GetCurrentDirectory());
                }

                if (CanCreateFolder)
                {
                    folders.Add(FormatEntry(":plus: ", CreateNewText), "///new");
                }


                foreach (string d in directoriesList)
                {
                    int cut = (ParentDir is not null) ? 1 : 0;
                    string FolderName = d.Substring((ActualFolder.Length) + cut);
                    string FolderPath = d;
                    if (CanDisplayIcons) folders.Add(":file_folder: " + FolderName, FolderPath);
                    else folders.Add(FolderName, FolderPath);
                }

                if (SelectFile)
                {
                    var fileList = Directory.GetFiles(ActualFolder);
                    foreach (string file in fileList)
                    {
                        string result = Path.GetFileName(file);
                        if (CanDisplayIcons) folders.Add(":abacus: " + result, file);
                        else folders.Add(result, file);
                    }
                }
                // We got two sets of lists list files and list folders
                string title = SelectFile ? SelectFileText : SelectFolderText;
                var selected = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[green]{title}:[/]")
                    .PageSize(PageSize)
                    .MoreChoicesText($"[grey]{MoreChoicesText}[/]")
                    .AddChoices(folders.Keys)
                );
                lastFolder = ActualFolder;

                record = folders.Where(s => s.Key == selected).Select(s => s.Value).FirstOrDefault() 
                                ?? throw new NullReferenceException("Selection is null");

                if (record == "/////")
                {
                    record = SelectDrive();
                    ActualFolder = record;
                }
                if (record == "///new")
                {
                    createNewFolder();
                }
                string responseType;
                if (Directory.Exists(record)) responseType = "Directory";
                else responseType = "File";

                if (record == Directory.GetCurrentDirectory())
                    return ActualFolder;
                if (responseType == "Directory")
                    try
                    {
                        ActualFolder = record; // How is this able to fail?
                    }
                    catch // what are we catching!?
                    {
                        AnsiConsole.WriteLine("[red]You have no access to this folder[/]");
                    }
                else
                    return record;
            }
        }

        public string GetFilePath(string ActualFolder)
        {
            return GetPath(ActualFolder, true);
        }

        public string GetFilePath()
        {
            return GetPath(ActualFolder, true);
        }

        public  string GetFolderPath(string ActualFolder)
        {
            return GetPath(ActualFolder, false);
        }

        public string GetFolderPath()
        {
            return GetPath(ActualFolder, false);
        }

        private string SelectDrive()
        {
            Drives = Directory.GetLogicalDrives();
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string drive in Drives)
            {
                if (CanDisplayIcons)
                    result.Add(":computer_disk: " + drive, drive);
                else
                    result.Add(drive, drive);
            }
            AnsiConsole.Clear();
            AnsiConsole.WriteLine();
            var rule = new Rule($"[b][green]{SelectDriveText}[/][/]").Centered();
            AnsiConsole.Write(rule);

            AnsiConsole.WriteLine();
            string title = SelectDriveText;
            string selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[green]{title}:[/]")
                .PageSize(PageSize)
                .MoreChoicesText($"[grey]{MoreChoicesText}[/]")
                .AddChoices(result.Keys)
            );
            // record returns the selected drive?
            string record = result.Where(s => s.Key == selected).Select(s => s.Value).FirstOrDefault()
                            ?? throw new NullReferenceException("Selection is null");
            return record;
        }
    }
}