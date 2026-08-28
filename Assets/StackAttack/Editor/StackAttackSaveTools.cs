using System;
using System.IO;
using StackAttack.Systems;
using UnityEditor;
using UnityEngine;

namespace StackAttack.Editor
{
    public static class StackAttackSaveTools
    {
        private const string MenuPath =
            "Tools/Stack Attack/Delete Saved Data";

        [MenuItem(MenuPath)]
        public static void DeleteSavedData()
        {
            var savePath = SaveManager.DefaultSavePath;
            if (!File.Exists(savePath))
            {
                EditorUtility.DisplayDialog(
                    "Delete Saved Data",
                    $"No saved data exists at:\n{savePath}",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Delete Saved Data",
                    $"Permanently delete the saved game data at:\n{savePath}",
                    "Delete",
                    "Cancel"))
            {
                return;
            }

            try
            {
                File.Delete(savePath);
                Debug.Log($"Deleted saved game data at '{savePath}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Could not delete saved game data at '{savePath}': " +
                    exception.Message);
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool CanDeleteSavedData()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }
    }
}
