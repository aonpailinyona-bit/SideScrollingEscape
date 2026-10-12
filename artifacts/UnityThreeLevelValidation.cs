using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class GameCompletenessAudit
{
    const string Prefix = "CompletenessAudit.";
    static double started;
    static readonly List<string> errors = new List<string>();
    [Serializable] public class Result { public string scene; public int players, killers, keys, exits, missingScripts; public bool loaded; public string[] errors; }
    [Serializable] public class Report { public Result[] levels; }
    static GameCompletenessAudit()
    {
        EditorApplication.playModeStateChanged += Changed;
        Application.logMessageReceived += Log;
        EditorApplication.delayCall += Resume;
    }
    static string ReportPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts/unity-three-level-validation.json"));
    static void Log(string message, string stack, LogType type)
    { if (EditorApplication.isPlaying && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) errors.Add(message); }
    static void Resume()
    {
        if (File.Exists(ReportPath) || SessionState.GetBool(Prefix+"Done", false)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Resume; return; }
        if (EditorApplication.isPlaying) { if (SessionState.GetBool(Prefix+"Active",false)) StartCapture(); return; }
        RunNext();
    }
    static void RunNext()
    {
        int i = SessionState.GetInt(Prefix+"Index",0);
        if (i >= 3)
        {
            var results = new List<Result>();
            for (int j=0;j<3;j++) results.Add(JsonUtility.FromJson<Result>(SessionState.GetString(Prefix+"Result"+j,"{}")));
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report {levels=results.ToArray()},true));
            SessionState.SetBool(Prefix+"Done",true);
            SessionState.SetBool(Prefix+"Active",false);
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            Debug.Log("THREE_LEVEL_VALIDATION_COMPLETE: "+ReportPath);
            return;
        }
        SessionState.SetBool(Prefix+"Active",true);
        EditorSceneManager.OpenScene("Assets/Scenes/Level"+(i+1)+".unity");
        EditorApplication.isPlaying=true;
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Prefix+"Active",false)) return;
        if (state==PlayModeStateChange.EnteredPlayMode) StartCapture();
        if (state==PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += RunNext;
    }
    static void StartCapture() { errors.Clear(); started=EditorApplication.timeSinceStartup; EditorApplication.update -= Capture; EditorApplication.update += Capture; }
    static void Capture()
    {
        if (EditorApplication.timeSinceStartup-started<3) return;
        EditorApplication.update -= Capture;
        int i=SessionState.GetInt(Prefix+"Index",0);
        var result=new Result { scene=SceneManager.GetActiveScene().name,loaded=SceneManager.GetActiveScene().isLoaded };
        foreach(var b in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if(b==null)continue;
            switch(b.GetType().Name)
            {
                case "PlayerController":result.players++;break;
                case "KillerAI":result.killers++;break;
                case "ExitDoor":result.exits++;break;
                case "ItemPickup":
                    var prop=b.GetType().GetProperty("ItemData");var item=prop?.GetValue(b);
                    if(item!=null && (bool)item.GetType().GetProperty("IsKeyItem").GetValue(item))result.keys++;
                    break;
            }
        }
        foreach(var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None)) result.missingScripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        result.errors=errors.ToArray();
        SessionState.SetString(Prefix+"Result"+i,JsonUtility.ToJson(result));
        SessionState.SetInt(Prefix+"Index",i+1);
        EditorApplication.isPlaying=false;
    }
}
