using System.Globalization;
using System.Numerics;

using Raylib_CsLo;

namespace SharpMania;


public static class Screen
{
    public static readonly int width = 400;
    public static readonly int height = 240;
    public static int Scale => Math.Min(Raylib.GetScreenWidth() / width, Raylib.GetScreenHeight() / height);
}

public class Program
{
    private static RenderTexture gameBuffer;

    public static void Main()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE | ConfigFlags.FLAG_WINDOW_MAXIMIZED);
        Raylib.InitWindow(Screen.width, Screen.height, "SharpMania");
        Raylib.SetWindowMinSize(Screen.width, Screen.height);
        Raylib.MaximizeWindow();
        Raylib.InitAudioDevice();
        Raylib.SetTargetFPS(60);

        gameBuffer = Raylib.LoadRenderTexture(Screen.width, Screen.height);
        Raylib.SetTextureFilter(gameBuffer.texture, TextureFilter.TEXTURE_FILTER_POINT);

        GameResources.Load();

        RayGui.GuiLoadStyle("resources/gui-style.rgs");

        SceneMediator.EnterSetupScene();


        Raylib.SetExitKey(KeyboardKey.KEY_NULL);
        while (!Raylib.WindowShouldClose())
        {
            Update();
            Draw();
        }

        SceneMediator.Unload();

        GameResources.Unload();
        Raylib.UnloadRenderTexture(gameBuffer);
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }

    private static void Update()
    {
        int scale = Screen.Scale;
        Vector2 mouse = Raylib.GetMousePosition();
        Vector2 virtualMouse = new()
        {
            X = (mouse.X - (Raylib.GetScreenWidth() - (Screen.width * scale)) * 0.5f) / scale,
            Y = (mouse.Y - (Raylib.GetScreenHeight() - (Screen.height * scale)) * 0.5f) / scale
        };
        virtualMouse = Vector2.Clamp(virtualMouse, Vector2.Zero, new (Screen.width, Screen.height));

        Raylib.SetMouseOffset(-(int)((Raylib.GetScreenWidth() - (Screen.width * scale)) * 0.5f), -(int)((Raylib.GetScreenHeight() - (Screen.height * scale)) * 0.5f));
        Raylib.SetMouseScale(1f/scale, 1f/scale);

        if (SceneMediator.CurrentScene != null)
        {
            SceneMediator.CurrentScene.Update();
        }
    }

    private static void Draw()
    {
        int scale = Screen.Scale;

        Raylib.BeginTextureMode(gameBuffer);
        {
            Raylib.ClearBackground(Colors.White);
            if (SceneMediator.CurrentScene != null)
            {
                SceneMediator.CurrentScene.Draw();
            }
        }
        Raylib.EndTextureMode();

        Raylib.BeginDrawing();
        {
            Raylib.ClearBackground(Colors.Black);
            Raylib.DrawTexturePro(
                gameBuffer.texture, 
                new Rectangle(0, 0, Screen.width, -Screen.height),
                new Rectangle((Raylib.GetScreenWidth() - Screen.width * scale) * 0.5f, (Raylib.GetScreenHeight() - Screen.height * scale) * 0.5f, Screen.width * scale, Screen.height * scale),
                new Vector2(0, 0), 
                0, 
                Colors.White);
        }
        Raylib.EndDrawing();
    }
}
