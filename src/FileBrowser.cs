using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

// make attributes priate instead of all being public
// Can't select drive on linux currently

namespace FileBrowser;  

public enum PromptType
{
    SETTING,
    FOLDER,
    FILE
}

public class UserPromptItem
{
    public PromptType promptType;
    public string text;
    public string? directory;

    public UserPromptItem(PromptType type, string text, string? directory=null)
    {
        this.promptType = type;
        this.text = text;
        this.directory = directory;
    }
}

public class Browser
{
    public bool _canDisplayIcons { get; set; } = true;
    private bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private Dictionary<string, string> _selectionDict = [];
    private List<UserPromptItem> SelectableItems = [];
    private string[]? _drives { get; set; }
    private string _record = "";

    private BrowserUI browserUI;
    private BrowserController browserController;

    public int PageSize { get; set; } = 15;
    public bool CanCreateFolder { get; set; } = true;
    public string WorkingDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public string LevelUpText { get; set; } = "Go to upper level";
    public string ActualFolderText { get; set; } = "Selected Folder";
    public string MoreChoicesText { get; set; } = "Use arrows Up and Down to select";
    public string CreateNewText { get; set; } = "Create new folder";
    public string SelectFileText { get; set; } = "Select File";
    public string SelectFolderText { get; set; } = "Select Folder";
    public string SelectDriveText { get; set; } = "Select Drive";
    public string SelectActualText { get; set; } = "Select Actual Folder";

    public Action? testAction;
    
    private Dictionary<string, Action?> Settings;

    public Browser()
    {
        browserController = new BrowserController();
        browserUI = new BrowserUI();

        Settings = new Dictionary<string, Action?>
        {
            {"testString", testAction}
        };
    }
    private string GetPath(string startingPath, bool canIncludeFiles)
    {
        WorkingDirectory = startingPath;
        string headerText = canIncludeFiles ? SelectFileText : SelectFolderText;

        while (true)
        {  
            Directory.SetCurrentDirectory(WorkingDirectory);
            DirectoryInfo? ParentDirectory = new DirectoryInfo(WorkingDirectory).Parent; // do we need the parent dir right now?

            string[] directoriesList =  Directory.GetDirectories(WorkingDirectory);
            string[] fileList = Directory.GetFiles(WorkingDirectory);
            
            AnsiConsole.Clear();
            AnsiConsole.WriteLine();

            var rule = new Rule($"[b][green]{headerText}[/][/]").Centered();

            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();
            AnsiConsole.Markup($"[b][Yellow]{ActualFolderText}: [/][/]");

            var DirectoryPath = new TextPath(WorkingDirectory.ToString());

            DirectoryPath.RootStyle = new Style(foreground: Color.Green);
            DirectoryPath.SeparatorStyle = new Style(foreground: Color.Green);
            DirectoryPath.StemStyle = new Style(foreground: Color.Blue);
            DirectoryPath.LeafStyle = new Style(foreground: Color.Yellow);
            AnsiConsole.Write(DirectoryPath);
            AnsiConsole.WriteLine();

            // get list of drives
            // key = the file/folder name
            // element = path to file/folder

            string temp; // not great, needs improving
            if (_isWindows)
            {
                temp = FormatSelectorEntry(":computer_disk:", SelectDriveText);
                SelectableItems.Add(new UserPromptItem(PromptType.SETTING, temp, "/////")); // new
            }

            if (ParentDirectory is not null)
            {
                temp = FormatSelectorEntry(":upwards_button: ", LevelUpText);
                SelectableItems.Add(new UserPromptItem(PromptType.SETTING, temp, ParentDirectory.FullName)); // new
            }

            if (!canIncludeFiles)
            {
                temp = FormatSelectorEntry(":ok_button: ", SelectActualText);
                SelectableItems.Add(new UserPromptItem(PromptType.SETTING, temp, WorkingDirectory)); // new
            }

            if (CanCreateFolder)
            {
                temp = FormatSelectorEntry(":plus: ", CreateNewText);
                SelectableItems.Add(new UserPromptItem(PromptType.SETTING, temp, "///new")); // new
            }
            
            // add folders

            // Old Code \/
            foreach (string directory in directoriesList)
            {
                temp = FormatItemEntry(":file_folder:", Path.GetFileName(directory));
                SelectableItems.Add(new UserPromptItem(PromptType.FILE, Path.GetFileName(temp), directory)); // new
                
            }

            // add files
            if (canIncludeFiles)
            {
                foreach (string file in fileList) // too unclear. what is file?? 
                {
                    temp = FormatItemEntry(":abacus:", Path.GetFileName(file));
                    SelectableItems.Add(new UserPromptItem(PromptType.FILE, Path.GetFileName(temp), file)); // new
                }
            }
            // Old Code /\

            // the problem is that this selection can either be a folder, file or setting. The need to be seperated into seperate objects
            // like setting.[setting], folder.getName, file.getName, maybe... At least seperate setting and folders/files
            //string userSelection = PromptSelectedFolder(_selectionDict, headerText);
            UserPromptItem newSelection = PromptSelectedFolder(SelectableItems, headerText);
            //string userSelection = newSelection.text;
            //Console.WriteLine("User selection :" + userSelection);

            //_record = _selectionDict.Where(s => s.Key == userSelection).Select(s => s.Value).FirstOrDefault() 
            //                ?? throw new NullReferenceException("Selection is null");

            if (newSelection.directory == "/////")
            {
                _record = SelectDrive();
                WorkingDirectory = _record;
            }

            if (newSelection.directory == "///new")
            {
                CreateNewFolder();
            }

            if (newSelection.directory == WorkingDirectory)
                return WorkingDirectory;

            if (Directory.Exists(newSelection.directory))
            {
                WorkingDirectory = newSelection.directory;
            }

            else 
            {
                return newSelection.directory;
            }; 

            // clear select dict. needs fixing
            _selectionDict = [];
            SelectableItems = [];
        }
    }
    public string GetFilePath(string workingDirectory)
    {
        return GetPath(workingDirectory, true);
    }
    public string GetFilePath()
    {
        return GetPath(WorkingDirectory, true);
    }
    public  string GetFolderPath(string workingDirectory)
    {
        return GetPath(workingDirectory, false);
    }
    public string GetFolderPath()
    {
        return GetPath(WorkingDirectory, false);
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

    private string ChangeWorkingDirectory(string currentDirectory, string folder)
    {
        return currentDirectory + "/" + folder; // if the path goes deeper into the tree
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

    private UserPromptItem PromptSelectedFolder(List<UserPromptItem> itemList, string selectorTitle)
    {   
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        Rule rule = new Rule($"[b][green]{selectorTitle}[/][/]").Centered();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
        return AnsiConsole.Prompt(
            new SelectionPrompt<UserPromptItem>()
                .Title($"[green]{selectorTitle}:[/]")
                .PageSize(PageSize)
                .MoreChoicesText($"[grey]{MoreChoicesText}[/]")
                .UseConverter(x =>
                {
                    if (x.promptType == PromptType.SETTING) { return x.text; }
                    else { return Markup.Escape(x.text); }
                })
                .AddChoices(itemList)
        );
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
                _record = Path.Combine(WorkingDirectory, folderName);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine("[red]Error: [/]" + ex.Message);
            }
        }
    }
}