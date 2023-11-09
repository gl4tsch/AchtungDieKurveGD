using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public Node CurrentScene { get; private set; }

    public override void _Ready()
    {
        base._Ready();
        if (Instance != null && Instance != this)
        {
            Free();
            return;
        }
        Instance = this;
    }

    public void GoToScene(string path)
    {
        CallDeferred(MethodName.DeferredGoToScene, path);
    }

    void DeferredGoToScene(string path)
    {
        CurrentScene?.Free();
        var nextScene = GD.Load<PackedScene>(path);
        CurrentScene = nextScene.Instantiate();

        GetTree().Root.AddChild(CurrentScene);
        // Optionally, to make it compatible with the SceneTree.change_scene_to_file() API.
        GetTree().CurrentScene = CurrentScene;
    }
}
