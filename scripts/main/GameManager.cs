using Godot;
using System.Collections.Generic;
using PlayerDataType = Godot.Collections.Dictionary;
//System.Collections.Generic.Dictionary<string,
//                             System.Collections.Generic.Dictionary<string,
//                                 System.Collections.Generic.Dictionary<string, int>>>;

public class GameManager : Node2D
{
    [Export]
    private Dictionary<string, int> _maxLevel; //= new Dictionary<string, int>() { { "Easy", 3 }, { "Medium", 0 }, { "Hard", 0 } };
    public Dictionary<string, int> MaxLevel { get { return _maxLevel; } }
    private Level _currentLevel;
    private int _currentLevelNumber = 0;
    public int CurrentLevelNumber { get { return _currentLevelNumber; }}
    private string _currentLevelType = "Easy";
    public string CurrentLevelType { get { return _currentLevelType; } }
    private string[] _levelTypes = { "Easy", "Medium", "Hard" };
    private Dictionary<string, string> _levelChain = new Dictionary<string, string>()  { { "Easy", "Medium" }, { "Medium", "Hard" },{ "Hard", null }};
    private Godot.Collections.Dictionary _levelLockDictionary = new Godot.Collections.Dictionary() { };
    private string _levelLockPath = "user://levelLock.dat";
    private PlayerDataType _playerDataDictionary = new PlayerDataType() { };
    private string _playerDataPath = "user://playerData.dat";
    public PlayerDataType PlayerDataDictionary { get { return _playerDataDictionary; } }
    public Godot.Collections.Dictionary LevelLockDictionary { get { return _levelLockDictionary; } }
    public string LevelLockPath { get { return _levelLockPath; } }
    public string PlayerDataPath { get { return _playerDataPath; } }


    [Signal]
    public delegate void LevelCompleted(int level, int stars, int moves, int best);
    [Signal]
    public delegate void QuitPressed();



    public void LoadDefaultPlayerData()
    {
        Dictionary<string, int> dataDictionary = new Dictionary<string, int>() { { "Stars", 0 }, { "Best", -1 } };
        _levelLockDictionary = new Godot.Collections.Dictionary() { };
        _playerDataDictionary = new PlayerDataType() { };

        foreach (string type in _levelTypes)
        {
            Dictionary<string, Dictionary<string, int>> typeDictionary = new Dictionary<string, Dictionary<string, int>>();
            Godot.Collections.Dictionary levelDataDictionary = new Godot.Collections.Dictionary();

            if (type == "Easy")
            {
                levelDataDictionary.Add("Unlocked", true);
            }
            else
            {
                levelDataDictionary.Add("Unlocked", false);
            }

            levelDataDictionary.Add("Completed", 0);
            _levelLockDictionary.Add(type, levelDataDictionary);


            for (int i = 0; i < _maxLevel[type]; i++)
            {
                typeDictionary.Add($"Level{i}", dataDictionary);
            }
            _playerDataDictionary.Add(type, typeDictionary);
        }


    }
    public void LoadPlayerData()
    {

        File file1 = new File();
        File file2 = new File();
        Error err1 = file1.Open(_levelLockPath, File.ModeFlags.Read);
        Error err2 = file2.Open(_playerDataPath, File.ModeFlags.Read);
        if (err1 != 0 || err2 != 0)
        {
            GD.Print("Error: loading player data");
            LoadDefaultPlayerData();
            return;
        }

        _levelLockDictionary = (Godot.Collections.Dictionary)file1.GetVar();
        _playerDataDictionary = (PlayerDataType)file2.GetVar();

        file1.Close();
        file2.Close();
    }
    public void SavePlayerData()
    {
        File file1 = new File();
        File file2 = new File();
        Error err1 = file1.Open(_levelLockPath, File.ModeFlags.Write);
        Error err2 = file2.Open(_playerDataPath, File.ModeFlags.Write);
        if (err1 != 0 || err2 != 0)
        {
            GD.Print("Error: saving player data");
            return;
        }
        file1.StoreVar(_levelLockDictionary);
        file2.StoreVar(_playerDataDictionary);
        file1.Close();
        file2.Close();
    }

    public int GetStars(string type, int level)
    {
        Godot.Collections.Dictionary dict1 = (Godot.Collections.Dictionary)_playerDataDictionary[type];
        Godot.Collections.Dictionary dict2 = (Godot.Collections.Dictionary)dict1[$"Level{level}"];
        return (int)dict2["Stars"];
    }
    public void SetStars(string type, int level, int stars)
    {
        Godot.Collections.Dictionary dict1 = (Godot.Collections.Dictionary)_playerDataDictionary[type];
        Godot.Collections.Dictionary dict2 = (Godot.Collections.Dictionary)dict1[$"Level{level}"];
        dict2["Stars"] = stars;
    }
    public int GetData(string type, int level, string data)
    {
        Godot.Collections.Dictionary dict1 = (Godot.Collections.Dictionary)_playerDataDictionary[type];
        Godot.Collections.Dictionary dict2 = (Godot.Collections.Dictionary)dict1[$"Level{level}"];
        return (int)dict2[data];
    }
    public void SetData(string type, int level, string dataType, int data)
    {
        Godot.Collections.Dictionary dict1 = (Godot.Collections.Dictionary)_playerDataDictionary[type];
        Godot.Collections.Dictionary dict2 = (Godot.Collections.Dictionary)dict1[$"Level{level}"];
        dict2[dataType] = data;
    }


    public bool IsLevelUnlocked(string type)
    {
        Godot.Collections.Dictionary dict1 = (Godot.Collections.Dictionary)_levelLockDictionary[type];
        return (bool)dict1["Unlocked"];
    }
    public void SetLevelUnlocked(string type)
    {
        Godot.Collections.Dictionary dict1 = (Godot.Collections.Dictionary)_levelLockDictionary[type];
        dict1["Unlocked"] = true;
    }

    public int NumberOfLevel(string type)
    {
        return _maxLevel[type];
    }
    public int NumberOfCompleted(string type)
    {
        Godot.Collections.Dictionary dict1 = (Godot.Collections.Dictionary)_levelLockDictionary[type];
        return (int)dict1["Completed"];
    }
    public void AddCompleted(string type)
    {
        Godot.Collections.Dictionary dict1 = (Godot.Collections.Dictionary)_levelLockDictionary[type];
        dict1["Completed"] = (int)dict1["Completed"] + 1;
    }


    public void LoadLevel(int levelNumber)
    {

        string path = $"res://scene/levels/Level{levelNumber}.tscn";
        PackedScene scene = ResourceLoader.Load<PackedScene>(path);

        Level level = scene.Instance<Level>();
        level.Number = levelNumber;
        level.Name = "Level";
        AddChild(level);
        level.Connect(nameof(Level.GameCompleted), this, nameof(_on_Level_GameCompleted));

        _currentLevel = level;
        _currentLevelNumber = levelNumber;
        _currentLevelType = level.Type;
    }
    public void NextLevel() => LoadLevel(_currentLevelNumber + 1);
    public void UnLockNextType(string type)
    {
        string nextType = _levelChain[type];
        if (nextType != null)
        {
            SetLevelUnlocked(nextType);
        }
    }
    public bool UnlockedCondtion(string type)
    {
        return (NumberOfCompleted(type) == _maxLevel[type]);
    }



    public override void _Ready()
    {
        LoadPlayerData();
        // LoadDefaultPlayerData(); SavePlayerData();
    }


    public void _on_Level_GameCompleted(int stars, int movesCounter)
    {
        string type = _currentLevel.Type;

        int highscore = GetData(type, _currentLevelNumber, "Best");
        if (highscore == -1)
        {
            SetData(type, _currentLevelNumber, "Best", movesCounter);
            AddCompleted(type);
            if (UnlockedCondtion(type))
            {
                UnLockNextType(type);
            }
        }
        else if (highscore > movesCounter)
        {
            SetData(type, _currentLevelNumber, "Best", movesCounter);
        }

        if (GetStars(type, _currentLevelNumber) < stars)
        {
            SetStars(type, _currentLevelNumber, stars);
        }
        
        SavePlayerData();
        _currentLevel.QueueFree();

        EmitSignal(nameof(LevelCompleted), _currentLevelNumber, stars, movesCounter, GetData(type, _currentLevelNumber, "Best"));
    }
    public void _on_PauseMenu_button_pressed(string name, PauseMenu pauseMenu)
    {
        pauseMenu.Hide();
        GetTree().Paused = false;
        
        switch (name)
        {
            case "Resume":
                _currentLevel.ScaleModulate(0.75f, false);
                break;
            case "Quit":
                _currentLevel.QueueFree();
                EmitSignal(nameof(QuitPressed));
                break;
        }
    }



}



