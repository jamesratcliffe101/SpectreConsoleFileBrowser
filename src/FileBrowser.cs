using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

// make attributes priate instead of all being public
// Can't select drive on linux currently

namespace FileBrowser;  

    public class Browser
    {
    public bool _canDisplayIcons { get; set; } = true;
    private bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private Dictionary<string, string> _selectionDict = new ();
    private string[]? _drives { get; set; }
    private string _record = "";

    private BrowserUI browserUI;
    private BrowserController browserController;

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

    public Browser()
    {
        browserController = new BrowserController();
        browserUI = new BrowserUI();
    }
    private string GetPath(string startingPath, bool canIncludeFiles)
    {
        ActualFolder = startingPath;
        string headerText = canIncludeFiles ? SelectFileText : SelectFolderText;

        while (true)
        {  
            Directory.SetCurrentDirectory(ActualFolder);
            DirectoryInfo? ParentDir = new DirectoryInfo(ActualFolder).Parent;

            string currentDirectory = Directory.GetCurrentDirectory();
            string[] directoriesList =  Directory.GetDirectories(currentDirectory);
            
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

            // get list of drives
            // key = the file/folder name
            // element = path to file/folder

            if (_isWindows)
            {
                _selectionDict.Add(FormatSelectorEntry(":computer_disk:", SelectDriveText), "/////");
            }

            if (ParentDir is not null)
            {
                _selectionDict.Add(FormatSelectorEntry(":upwards_button: ", LevelUpText), ParentDir.FullName);
            }

            if (!canIncludeFiles)
            {
                _selectionDict.Add(FormatSelectorEntry(":ok_button: ", SelectActualText), currentDirectory);
            }

            if (CanCreateFolder)
            {
                _selectionDict.Add(FormatSelectorEntry(":plus: ", CreateNewText), "///new");
            }

            foreach (string d in directoriesList)
            {
                int cut = (ParentDir is not null) ? 1 : 0;
                string FolderName = d.Substring((ActualFolder.Length) + cut);
                string FolderPath = d;

                _selectionDict.Add(FormatItemEntry(":file_folder:", FolderName), FolderPath);
            }

            if (canIncludeFiles)
            {
                var fileList = Directory.GetFiles(ActualFolder);
                foreach (string file in fileList)
                {
                    string result = Path.GetFileName(file);

                    _selectionDict.Add(FormatItemEntry(":abacus:", result), file);
                }
            }

            // We got two sets of lists list files and list folders
            string title = canIncludeFiles ? SelectFileText : SelectFolderText;

            string userSelection = PromptSelectedFolder(_selectionDict, title);
            _record = _selectionDict.Where(s => s.Key == userSelection).Select(s => s.Value).FirstOrDefault() 
                            ?? throw new NullReferenceException("Selection is null");

            if (_record == "/////")
            {
                _record = SelectDrive();
                ActualFolder = _record;
            }

            if (_record == "///new")
            {
                CreateNewFolder();
            }

            if (_record == currentDirectory)
                return ActualFolder;

            if (Directory.Exists(_record))
            {
                ActualFolder = _record;
            }

            else 
            {
                return _record;
            }; 
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
        _drives = Directory.GetLogicalDrives();
        Dictionary<string, string> listOfDrives = new Dictionary<string, string>();
        foreach (string drive in _drives)
        {
            if (_canDisplayIcons)
                listOfDrives.Add(":computer_disk: " + drive, drive);
            else
                listOfDrives.Add(drive, drive);
        }
        
        string selected = PromptSelectedFolder(listOfDrives, SelectDriveText);
        // record returns the selected drive?
        _record = listOfDrives.Where(s => s.Key == selected).Select(s => s.Value).FirstOrDefault()
                        ?? throw new NullReferenceException("Selection is null");
        return _record;
    }


    /// <summary>
    /// Draws the file browser content to the screen using SpectreConsole
    /// </summary>
    private string PromptSelectedFolder(Dictionary<string, string> itemList, string selectorTitle)
    {   
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        Rule rule = new Rule($"[b][green]{selectorTitle}[/][/]").Centered();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[green]{selectorTitle}:[/]")
                .PageSize(PageSize)
                .MoreChoicesText($"[grey]{MoreChoicesText}[/]")
                .AddChoices(itemList.Keys)
        );
    }

    private void ProcessSelection(string selectedItem)
    {
        _record = _selectionDict.Where(s => s.Key == selectedItem).Select(s => s.Value).FirstOrDefault() 
                            ?? throw new NullReferenceException("Selection is null");


    }


    /// <summary>
    /// Creates formatted string for optional entries in folder selector list like
    /// "go back" or "create folder"
    /// </summary>
    /// <param name="icon"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    private string FormatSelectorEntry(string icon, string text)
    {
        return $"{(_canDisplayIcons ? icon : "")}[green]{text}[/]";
    }

    /// <summary>
    /// Creates formatted string for folders and files
    /// </summary>
    /// <param name="icon"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    private string FormatItemEntry(string icon, string text)
    {
        return $"{(_canDisplayIcons ? icon : "")}{text}";
    }
 
        
    /// <summary>
    /// Asks user for folder name then creates a new folder with given name
    /// </summary>
    private void CreateNewFolder()
    {
        string folderName = AnsiConsole.Ask<string>("[blue]" + CreateNewText + ": [/]");
        if (folderName != null)
        {
            try
            {
                Directory.CreateDirectory(folderName);
                _record = Path.Combine(ActualFolder, folderName);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine("[red]Error: [/]" + ex.Message);
            }
        }
    }
}