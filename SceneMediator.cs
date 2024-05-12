
namespace SharpMania;

public static class SceneMediator
{
    public static Scene? CurrentScene => currentScene;
    private static Scene? currentScene = null;

    private static HashSet<Scene> loadedScenes = new();

    private static SetupScene? setupScene;


    public static void EnterSetupScene()
    {
        TryUnloadScene();
        if (setupScene == null)
        {
            setupScene = new();
            LoadScene(setupScene);
            return;
        }
        currentScene = setupScene;
    }

    public static void EnterTrackScene(Track track, TrackMap map, GameOptions options)
    {
        var scene = new TrackScene(track, map, options);
        LoadScene(scene);
    }

    public static void Unload()
    {
        foreach (var scene in loadedScenes)
        {
            scene.Unload();
        }
        currentScene = null;
        loadedScenes.Clear();
    }

    private static void LoadScene(Scene scene)
    {
        currentScene = scene;
        loadedScenes.Add(scene);
        scene.Load();
    }

    private static void UnloadScene()
    {
        if (!TryUnloadScene()) throw new Exception("There is no scene to unload");
    }

    private static bool TryUnloadScene()
    {
        if (currentScene == null) return false;
        currentScene.Unload();
        loadedScenes.Remove(currentScene);
        currentScene = null;
        return true;
    }
}
