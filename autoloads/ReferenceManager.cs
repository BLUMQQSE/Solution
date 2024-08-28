using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public partial class ReferenceManager : Node
{
    private static ReferenceManager instance = null;

    private Dictionary<string, string> scriptPaths = new Dictionary<string, string>();

    private Dictionary<string, string> levelPaths = new Dictionary<string, string>();
    private Dictionary<string, string> scenePaths = new Dictionary<string, string>();

    private Dictionary<string, string> texturePaths = new Dictionary<string, string>();
    private Dictionary<string, string> modelPaths = new Dictionary<string, string>();

    private Dictionary<string, string> audioPaths = new Dictionary<string, string>();

    private Dictionary<string, string> resourcePaths = new Dictionary<string, string>();

    private Dictionary<string, string> shaderPaths = new Dictionary<string, string>();

    // optimization. Cuts loading scenes by over half.
    public Dictionary<string, Script> Scripts = new Dictionary<string, Script>();

    public Dictionary<string, string> ScenePaths { get { return scenePaths; } }
    public Dictionary<string, string> LevelPaths { get { return levelPaths; } }
    private ReferenceManager()
    {
        if(instance == null)
            instance = this;
        AddToGroup(SNC.Groups[(int)Globals.Groups.AutoLoad]);
    }

    public static ReferenceManager Instance { get { return instance; } }

    public override void _Ready()
    {
        if (OS.HasFeature("editor"))
        {
            SetDicts();
            JsonValue root = new JsonValue();

            foreach (KeyValuePair<string, string> script in Instance.scriptPaths)
                root["scripts"].Add(script.Key, script.Value);
            foreach (KeyValuePair<string, string> scene in Instance.scenePaths)
                root["scenes"].Add(scene.Key, scene.Value);
            foreach (KeyValuePair<string, string> level in Instance.levelPaths)
                root["levels"].Add(level.Key, level.Value);
            foreach (KeyValuePair<string, string> texture in Instance.texturePaths)
                root["textures"].Add(texture.Key, texture.Value);
            foreach (KeyValuePair<string, string> model in Instance.modelPaths)
                root["models"].Add(model.Key, model.Value);
            foreach (KeyValuePair<string, string> resource in Instance.resourcePaths)
                root["resources"].Add(resource.Key, resource.Value);
            foreach (KeyValuePair<string, string> audio in Instance.audioPaths)
                root["audio"].Add(audio.Key, audio.Value);
            foreach(KeyValuePair<string, string> shader in Instance.shaderPaths)
                root["shaders"].Add(shader.Key, shader.Value);

            FileManager.SaveToFileFormatted(root, "ReferenceManager.json", FileType.Res);
        }
        else
        {
            JsonValue root = FileManager.LoadFromFile("ReferenceManager.json", FileType.Res);

            foreach (KeyValuePair<string, JsonValue> pair in root["scripts"].Object)
                Instance.scriptPaths[pair.Key] = pair.Value.AsString();
            foreach (KeyValuePair<string, JsonValue> pair in root["scenes"].Object)
                Instance.scenePaths[pair.Key] = pair.Value.AsString();
            foreach (KeyValuePair<string, JsonValue> pair in root["levels"].Object)
                Instance.levelPaths[pair.Key] = pair.Value.AsString();
            foreach (KeyValuePair<string, JsonValue> pair in root["textures"].Object)
                Instance.texturePaths[pair.Key] = pair.Value.AsString();
            foreach (KeyValuePair<string, JsonValue> pair in root["models"].Object)
                Instance.modelPaths[pair.Key] = pair.Value.AsString();
            foreach (KeyValuePair<string, JsonValue> pair in root["resources"].Object)
                Instance.resourcePaths[pair.Key] = pair.Value.AsString();
            foreach (KeyValuePair<string, JsonValue> pair in root["audio"].Object)
                Instance.audioPaths[pair.Key] = pair.Value.AsString();
            foreach (KeyValuePair<string, JsonValue> pair in root["shaders"].Object)
                Instance.shaderPaths[pair.Key] = pair.Value.AsString();
        }

        foreach(var c in scriptPaths)
        {
            if (c.Value.Contains("autload"))
                continue;
            if (c.Value.Contains("classes"))
                continue;
            if (c.Value.Contains("ui"))
                continue;
            if (c.Value.Contains("resources"))
                continue;
            Scripts[c.Key] = GD.Load<Script>(c.Value);
        }
    }

    /// <summary>Takes in a scene file name, and returns the full path from res://. The input
    /// string can include the .tscn extension or not.</summary>
    /// <returns>The full path extending beyond res:// where the scene is located. Ex.
    /// "town" returns "res://scenes/town.tscn"</returns>
    public string GetScenePath(string sceneName)
    {
        if (sceneName.EndsWith(".tcsn"))
            sceneName = sceneName.Substring(0, sceneName.Length - 5);

        if (scenePaths.TryGetValue(sceneName, out string path))
            return "res://" + path;


        GD.Print("[Warning][ResoureManager] Scene: " + sceneName + " cannot be detected");
        return String.Empty;
    }

    public string GetLevelPath(string levelName)
    {
        if (levelName.EndsWith(".tcsn"))
            levelName = levelName.Substring(0, levelName.Length - 5);

        if (levelPaths.TryGetValue(levelName, out string path))
            return "res://" + path;


        GD.Print("[Warning][ResoureManager] Level: " + levelName + " cannot be detected");
        return String.Empty;
    }
    /// <summary>Takes in a script file name, and returns the full path from res://. The input
    /// string can include the .cs extension or not.</summary>
    /// <returns>The full path extending beyond res:// where the script is located. Ex.
    /// "Console" returns "res://scripts/system/Console.cs"</returns>
    public string GetScriptPath(string scriptName)
    {
        if (scriptName.EndsWith(".cs"))
            scriptName = scriptName.Substring(0, scriptName.Length - 3);

        if (scriptPaths.TryGetValue(scriptName, out string path))
            return "res://" + path;

        GD.Print("[Warning][ResoureManager] Script: " + scriptName + " cannot be detected");
        return String.Empty;
    }

    public string GetTexturePath(string textName)
    {
        if (textName.EndsWith(".png"))
            textName = textName.Substring(0, textName.Length - 4);

        if (texturePaths.TryGetValue(textName, out string path))
            return "res://" + path;

        GD.Print("[Warning][ResoureManager] Texture: " + textName + " cannot be detected");

        return String.Empty;
    }

    public string GetModelPath(string modelName)
    {
        if (modelName.EndsWith(".obj") || modelName.EndsWith(".glb"))
            modelName = modelName.Substring(0, modelName.Length - 4);

        if (modelPaths.TryGetValue(modelName, out string path))
            return "res://" + path;

        GD.Print("[Warning][ResoureManager] Model: " + modelName + " cannot be detected");

        return String.Empty;
    }

    public string GetResourcePath(string resourceName)
    {
        if (resourceName.EndsWith(".tres"))
            resourceName = resourceName.Substring(0, resourceName.Length - 5);

        if (resourcePaths.TryGetValue(resourceName, out string path))
            return "res://" + path;

        GD.Print("[Warning][ResoureManager] Resource: " + resourceName + " cannot be detected");

        return String.Empty;
    }

    public string GetAudioPath(string audioName)
    {
        if (audioName.EndsWith(".wav"))
            audioName = audioName.Substring(0, audioName.Length - 4);

        if (audioPaths.TryGetValue(audioName, out string path))
            return "res://" + path;

        GD.Print("[Warning][ResoureManager] Audio: " + audioName + " cannot be detected");

        return String.Empty;
    }

    public string GetShaderPath(string shaderName)
    {
        if (shaderName.EndsWith(".gdshader"))
            shaderName = shaderName.Substring(0, shaderName.Length - 9);

        if (audioPaths.TryGetValue(shaderName, out string path))
            return "res://" + path;

        GD.Print("[Warning][ResoureManager] Shader: " + shaderName + " cannot be detected");

        return String.Empty;
    }

    private void SetDicts()
    {
        string[] filePaths = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\", "*", SearchOption.AllDirectories);

        foreach (string filePath in filePaths)
        {
            // ignore hidden files
            if (filePath[Directory.GetCurrentDirectory().Length + 1] == '.')
                continue;

            int suffixStart = filePath.RFind(".");
            string suffix = filePath.Substring(suffixStart);

            int index = filePath.RFind("\\") + 1;
            if (index == -1) continue;

            string key = "";
            string value = "";

            if (suffix == ".cs")
            {
                // script
                key = filePath.Substring(index, filePath.Length - index - 3);
                value = filePath.Replace(Directory.GetCurrentDirectory() + "\\", "");
                value = value.Replace("\\", "/");
                if (!Instance.scriptPaths.TryAdd(key, value))
                    GD.Print("[ReferenceManager] Error: Duplicate script names: " + key);
            }
            else if (suffix == ".tscn")
            {
                key = filePath.Substring(index, filePath.Length - index - 5);
                value = filePath.Replace(Directory.GetCurrentDirectory() + "\\", "");
                value = value.Replace("\\", "/");
                if (value.Contains("levels"))
                {
                    if (!Instance.levelPaths.TryAdd(key, value))
                        GD.Print("[ReferenceManager] Error: Duplicate prefab names: " + key);
                }
                else
                {
                    if (!Instance.scenePaths.TryAdd(key, value))
                        GD.Print("[ReferenceManager] Error: Duplicate scene names: " + key);
                }
            }
            else if (suffix == ".png")
            {
                // texture
                key = filePath.Substring(index, filePath.Length - index - 4);
                value = filePath.Replace(Directory.GetCurrentDirectory() + "\\", "");
                value = value.Replace("\\", "/");
                if (!Instance.texturePaths.TryAdd(key, value))
                    GD.Print("[ReferenceManager] Error: Duplicate texture names: " + key);
            }
            else if (suffix == ".glb" || suffix == ".obj")
            {
                // models
                key = filePath.Substring(index, filePath.Length - index - 4);
                value = filePath.Replace(Directory.GetCurrentDirectory() + "\\", "");
                value = value.Replace("\\", "/");
                if (!Instance.modelPaths.TryAdd(key, value))
                    GD.Print("[ReferenceManager] Error: Duplicate model names: " + key);
            }
            else if (suffix == ".tres")
            {
                // resource
                key = filePath.Substring(index, filePath.Length - index - 5);
                value = filePath.Replace(Directory.GetCurrentDirectory() + "\\", "");
                value = value.Replace("\\", "/");
                if (!Instance.resourcePaths.TryAdd(key, value))
                    GD.Print("[ReferenceManager] Error: Duplicate resource names: " + key);
            }
            else if (suffix == ".wav" || suffix == ".ogg")
            {
                // audio
                key = filePath.Substring(index, filePath.Length - index - 4);
                value = filePath.Replace(Directory.GetCurrentDirectory() + "\\", "");
                value = value.Replace("\\", "/");
                if (!Instance.audioPaths.TryAdd(key, value))
                    GD.Print("[ReferenceManager] Error: Duplicate audio names: " + key);
            }
            else if(suffix == ".gdshader")
            {
                // shader
                key = filePath.Substring(index, filePath.Length - index - 9);
                value = filePath.Replace(Directory.GetCurrentDirectory() + "\\", "");
                value = value.Replace("\\", "/");
                if (!Instance.shaderPaths.TryAdd(key, value))
                    GD.Print("[ReferenceManager] Error: Duplicate shader names: " + key);
            }
        }
    }


}
