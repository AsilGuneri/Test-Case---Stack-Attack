using System;
using System.IO;
using UnityEngine;
using VContainer;

namespace StackAttack.Systems
{
    [Serializable]
    public sealed class SaveFile
    {
        [SerializeField, Min(0)] private int lastCompletedLevel;

        public int LastCompletedLevel => lastCompletedLevel;

        internal void SetLastCompletedLevel(int levelNumber)
        {
            lastCompletedLevel = Mathf.Max(0, levelNumber);
        }
    }

    public interface ISaveManager
    {
        SaveFile Data { get; }
        string SavePath { get; }
        event Action<int> LastCompletedLevelChanged;

        void MarkLevelCompleted(int levelNumber);
        void Save();
        void Reload();
    }

    public sealed class SaveManager : ISaveManager
    {
        private const string FileName = "save.json";

        public static string DefaultSavePath =>
            Path.Combine(Application.persistentDataPath, FileName);
        public SaveFile Data { get; private set; }
        public string SavePath { get; }
        public event Action<int> LastCompletedLevelChanged;

        [Inject]
        public SaveManager()
            : this(DefaultSavePath)
        {
        }

        public SaveManager(string savePath)
        {
            if (string.IsNullOrWhiteSpace(savePath))
            {
                throw new ArgumentException(
                    "A save path is required.",
                    nameof(savePath));
            }

            SavePath = savePath;
            Reload();
        }

        public void MarkLevelCompleted(int levelNumber)
        {
            if (levelNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(levelNumber));
            }

            if (levelNumber <= Data.LastCompletedLevel)
            {
                return;
            }

            Data.SetLastCompletedLevel(levelNumber);
            Save();
            LastCompletedLevelChanged?.Invoke(Data.LastCompletedLevel);
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(
                    SavePath,
                    JsonUtility.ToJson(Data, true));
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Could not save game data to '{SavePath}': " +
                    exception.Message);
            }
        }

        public void Reload()
        {
            Data = LoadFile();
        }

        private SaveFile LoadFile()
        {
            if (!File.Exists(SavePath))
            {
                return new SaveFile();
            }

            try
            {
                var loaded = JsonUtility.FromJson<SaveFile>(
                    File.ReadAllText(SavePath)) ?? new SaveFile();
                loaded.SetLastCompletedLevel(loaded.LastCompletedLevel);
                return loaded;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Could not load game data from '{SavePath}'. " +
                    $"Starting fresh. {exception.Message}");
                return new SaveFile();
            }
        }
    }
}
