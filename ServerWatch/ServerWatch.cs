using Newtonsoft.Json;
using System;
using System.IO;
using PSLM;
using System.Diagnostics;

namespace ServerWatch;

public class ServerWatch
{
    Config _config;
    PSLM.PSLM pslm;
    ProcessWatchdog _processWatchdog;
    bool _exitApp = false;
    bool _initialStart = false;


    ServerWatch() // Constructor
    {

        pslm = new PSLM.PSLM("log-%yyyy%-%MM%-%dd%.txt", ".\\logs\\", "DEBUG", true, "dd-MM-yyyy HH:mm:ss.fff"); // Initialize PSLM ("log-%yyyy%-%MM%-%dd%.txt", ".\", "DEFAULT", $true, "time")
        pslm.Info("[ServerWatch::ServerWatch] PSLM initialized"); // Log message
        if (!CheckForConfig()) // Check if config file exists
        {
            pslm.Info("[ServerWatch::ServerWatch] No config file -> creating template");
            CreateDefaultConfig(); // Create default config if not found
            _initialStart = true; // Set initial start to true
        }

        if (_initialStart)
        {
            pslm.Info("[ServerWatch::ServerWatch] This is the inital start or config was deleted.");
            pslm.Warn("[SETUP] ===================================================");
            pslm.Warn("[SETUP] Please edit the config.json file and restart the program.");
            pslm.Warn("[SETUP] ===================================================");
            Thread.Sleep(100000);
        }
        else
        {
            pslm.Info("[ServerWatch::ServerWatch] Loading config");
            _config = LoadConfig(); // Load config from JSON
            InitProcessWatchdog(this);
        }

            


    }

    static int Main(string[] args) //Main Method
    {
        var serverWatch = new ServerWatch();

        if (serverWatch._initialStart)
        {
            return 0; //Close application, because inital start
        }

        serverWatch.pslm.Info("[ServerWatch::Main] Constructor finished");
        if (serverWatch._config.autostart) { serverWatch._processWatchdog.StartProcess(); }
        serverWatch.UserInputListener();
        serverWatch.ServerWatchMenu(false);

        while (!serverWatch._exitApp)
        {
            //serverWatch.ServerWatchMenu(false);
            Thread.Sleep(500); // einfach Leerlauf, weil alles über Threads läuft
        }
        serverWatch.pslm.Info("[ServerWatch::Main] Exited main loop. Closing...");
        serverWatch.PrintInfo();
        return 0; //smoothly close
    }


    void PrintInfo()
    {
        pslm.Debug($"[ServerWatch::PrintInfo] === PrintInfo === ");
        pslm.Debug($"[ServerWatch::PrintInfo] Server Name: {_config.serverName}");
        pslm.Debug($"[ServerWatch::PrintInfo] Working Directory: {_config.workingDir}");
        pslm.Debug($"[ServerWatch::PrintInfo] Executable: {_config.executable}");
        pslm.Debug($"[ServerWatch::PrintInfo] Parameters: {_config.parameters}");
        pslm.Debug($"[ServerWatch::PrintInfo] Behavior: {_config.behavior}");
        pslm.Debug($"[ServerWatch::PrintInfo] Autostart: {_config.autostart}");
        pslm.Debug($"[ServerWatch::PrintInfo] ================= ");
    }

    // Lädt Konfig aus JSON
    Config LoadConfig()
    {
        pslm.Debug($"[ServerWatch::LoadConfig] Start");
        string jsonFile = File.ReadAllText("config.json");
        pslm.Debug($"[ServerWatch::LoadConfig] File read");
        var config = JsonConvert.DeserializeObject<Config>(jsonFile);
        pslm.Debug($"[ServerWatch::LoadConfig] Deserialized object");
        pslm.Debug($"[ServerWatch::LoadConfig] Returning");
        Console.Title = $"ServerWatch - {config.serverName}";
        return config;
    }

    void ReloadConfig()
    {
        pslm.Info($"[ServerWatch::ReloadConfig] Reloading config...");
        _config = LoadConfig();
        pslm.Info($"[ServerWatch::ReloadConfig] Config reload complete");
        PrintInfo(); // Print info in debug mode
        _processWatchdog = null;
        InitProcessWatchdog(this);

    }

    void SaveConfig()
    {
        pslm.Debug($"[ServerWatch::SaveConfig] Start");
        string jsonFile = JsonConvert.SerializeObject(_config, Formatting.Indented);
        pslm.Debug($"[ServerWatch::SaveConfig] Serialized object");
        File.WriteAllText("config.json", jsonFile);
        pslm.Debug($"[ServerWatch::SaveConfig] Wrote to file");
        pslm.Debug($"[ServerWatch::SaveConfig] Done");
    }

    bool CheckForConfig()
    {
        if (File.Exists("config.json"))
        {
            pslm.Info($"[ServerWatch::CheckForConfig] Config file found.");
            return true;
        }
        else 
        {
            pslm.Warn($"[ServerWatch::CheckForConfig] Config file not found.");
            return false;
        }
    }

    void CreateDefaultConfig()
    {
        pslm.Info("[ServerWatch::CreateDefaultConfig] Creating default config.");
        _config = new Config
        {
            serverName = "DefaultServer",
            workingDir = "C:\\Default\\Path",
            executable = "server.exe",
            parameters = "",
            behavior = "restart",
            autostart = true
        };
        SaveConfig();
    }

    public void ServerWatchMenu(bool ClearConsole)
    {
        if (ClearConsole)
        {
            Console.Clear();
        }

        Console.WriteLine("=== ServerWatch Menu ===");
        Console.WriteLine("[Start]   : Start Server");
        Console.WriteLine("[Stop]    : Stop Server");
        Console.WriteLine("[Restart] : Restart Server");
        Console.WriteLine("[Reload]  : Reload Config");
        Console.WriteLine("[Help]    : Show this menu");
        Console.WriteLine("[Quit]    : Close ServerWatch");
        Console.Write("\n > ");
        //var input = Console.ReadLine();

    }

    void InitProcessWatchdog(ServerWatch serverWatch)
    {
        try
        {
            pslm.Debug("Initializing processWatchdog..");
            _processWatchdog = new ProcessWatchdog(this, _config, pslm);
            pslm.Info("Initialized processWatchdog");      
        }
        catch (Exception e)
        {
            pslm.Error($"Initializing processWatchdog failed! >> {e.Message}");
            Environment.Exit(10); // Error Code 10 = processWatchdog initialization failed
            throw;
        }
    }

    void UserInputListener()
    {
        new Thread(() =>
        {
            while (!_exitApp)
            {
                string input = Console.ReadLine();

                CommandHandler(input);
            }

        })
        { IsBackground = true }.Start();
    }

    void CommandHandler(string command)
    {
        switch (command.ToLower())
        {
            case "start":
                _processWatchdog.StartProcess();
                break;
            case "stop":
                _processWatchdog.StopProcess();
                break;
            case "restart":
                _processWatchdog.RestartProcess();
                break;
            case "quit" or "q":
                _exitApp = true;
                break;
            case "reload":
                ReloadConfig();
                break;
            case "help" or "h" or "menu" or "m":
                ServerWatchMenu(false);
                break;
            default:
                pslm.Error($"[ServerWatch::ServerWatchMenu] Input '{command}' is not a valid option! Use 'help' or 'h' to see commands.");
                //ServerWatchMenu();
                break;
        }
        

    }
}

// Config-Klasse für JSON
public class Config
{
    public string serverName { get; set; }
    public string workingDir { get; set; }
    public string executable { get; set; }
    public string parameters { get; set; }
    public string behavior { get; set; }
    public bool autostart { get; set; }
}

public class ProcessWatchdog
{
    Config _config;
    PSLM.PSLM pslm;
    Process _process;
    ServerWatch _serverWatch;
    bool _restartOnExit = false;
    bool _stopServer = false;

    public ProcessWatchdog(ServerWatch serverWatch, Config config, PSLM.PSLM logger) // Constructor
    {
        _serverWatch = serverWatch;
        _config = config;
        _restartOnExit = config.autostart;
        pslm = logger;   
    }

    public void StartProcess()
    {
        if (_process != null && !_process.HasExited)
        {
            pslm.Warn("[ProcessWatchdog::StartProcess] Process is already running!");
            return;
        }
        
        pslm.Info($"[ProcessWatchdog::StartProcess] Starting process: {_config.executable} {_config.parameters}");

        _process = new Process();
        string exePath = _config.workingDir+"\\"+_config.executable;
        pslm.Debug("[ProcessWatchdog::StartProcess] exePath: " + exePath);
        _process.StartInfo.FileName = exePath;
        _process.StartInfo.Arguments = _config.parameters;
        _process.StartInfo.WorkingDirectory = _config.workingDir;
        _process.EnableRaisingEvents = true;
        _process.Exited += OnProcessExited;

        try
        {
            _process.Start();
            pslm.Info("[ProcessWatchdog::StartProcess] Process started");
        }
        catch (Exception ex)
        {
            pslm.Error($"[ProcessWatchdog::StartProcess] Failed to start process: {ex.Message}");
        }
    }

    public void StopProcess()
    {
        if (_process == null || _process.HasExited)
        {
            pslm.Warn("[ProcessWatchdog::StopProcess] Process is not running!");
            return;
        }
        pslm.Debug("[ProcessWatchdog::StopProcess] Trying to kill process..");
        try
        {
            _process.Kill();
            pslm.Info("[ProcessWatchdog::StopProcess] Process killed");
            _restartOnExit = false; // Disable automatic restart through event listener
            pslm.Debug("[ProcessWatchdog::StopProcess] _restartOnExit set to false");
            _stopServer = true; // Disable automatic restart through event listener
            pslm.Debug("[ProcessWatchdog::StopProcess] _stopServer set to true");
        }
        catch (Exception ex)
        {
            pslm.Error($"[ProcessWatchdog::StopProcess] Process couldn't be killed >> {ex.Message}");
            throw;
        }
    }

    public void RestartProcess()
    {
        StopProcess();
        StartProcess();
    }

    void OnProcessExited(object sender, EventArgs e)
    {
        pslm.Crit("[ProcessWatcher::OnProcessExited] Process exited!");
        if(!_stopServer)
        {
            ServerCrashMessage();
        }
        
        if(_stopServer == false && _restartOnExit == true) // HIER CHECKEN OB RESTARTONEXIT ÜBERHAUPT GEBRACUHT WIRD!!
        {
            switch (_config.behavior.ToLower())
            {
                case "restart":
                    Restart();
                    break;
                case "stop":
                    Stop();
                    break;
                case "manual":
                    Manual();
                    break;
                case "notify":
                    Notify();
                    break;
                default:
                    Restart();
                    break;
            }
        }

        
    }

    void Restart()
    {
        pslm.Info("[ProcessWatcher::Restart] Initiating restart..");
        RestartProcess();
    }

    void Stop()
    {
        pslm.Info("[ProcessWatcher::Stop] Stopping after process exit");
        StopProcess();
    }

    void Manual()
    {
        pslm.Info("[ProcessWatcher::Manual] Waiting for user interaction..");
        PromptForUserInput();
    }

    void Notify()
    {
        pslm.Info("[ProcessWatcher::Notify] Waiting for user interaction..");
    }

    void PromptForUserInput()
    {
        _serverWatch.ServerWatchMenu(true);
    }

    void ServerCrashMessage()
    {
        pslm.Warn("[ProcessWatcher::NotifyAboutExit] ====================");
        pslm.Warn("[ProcessWatcher::NotifyAboutExit] === SERVER CRASH ===");
        pslm.Warn($"[ProcessWatcher::NotifyAboutExit] Exit Code: {_process.ExitCode}");
        pslm.Warn("[ProcessWatcher::NotifyAboutExit] ====================");
    }




}