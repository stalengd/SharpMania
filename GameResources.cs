using Raylib_CsLo;

namespace SharpMania;

public static class GameResources
{
    public static Texture arrowTex;
    public static Texture judgementArrowTex;
    public static Texture scoreMarksTex;
    public static Sound assistSound;
    public static Sound shotSound;
    public static Font lazyFox1Font;
    public static Font lazyFox2Font;

    private static List<Texture> textures = new();
    private static List<Sound> sounds = new();
    private static List<Music> musicStreams = new();
    private static List<Font> fonts = new();

    public static void Load()
    {
        arrowTex = LoadTexture("resources/textures/arrows.png");
        judgementArrowTex = LoadTexture("resources/textures/judgement-arrows.png");
        scoreMarksTex = LoadTexture("resources/textures/score-marks.png");
        assistSound = LoadSound("resources/sounds/GameplayAssist clap.ogg");
        shotSound = LoadSound("resources/sounds/shot-small-1.wav");
        lazyFox1Font = LoadFont("resources/fonts/LazyFox Pixel 1.ttf", 11);
        lazyFox2Font = LoadFont("resources/fonts/LazyFox Pixel 2.ttf", 11);
    }

    public static void Unload()
    {
        for (int i = 0; i < textures.Count; i++)
        {
            Raylib.UnloadTexture(textures[i]);
        }
        for (int i = 0; i < sounds.Count; i++)
        {
            Raylib.UnloadSound(sounds[i]);
        }
        for (int i = 0; i < musicStreams.Count; i++)
        {
            Raylib.UnloadMusicStream(musicStreams[i]);
        }
        for (int i = 0; i < fonts.Count; i++)
        {
            Raylib.UnloadFont(fonts[i]);
        }
        textures.Clear();
        sounds.Clear();
        musicStreams.Clear();
        fonts.Clear();
    }

    private static Texture LoadTexture(string path)
    {
        var texture = Raylib.LoadTexture(path);
        textures.Add(texture);
        return texture;
    }
    private static Sound LoadSound(string path)
    {
        var sound = Raylib.LoadSound(path);
        sounds.Add(sound);
        return sound;
    }
    private static Music LoadMusic(string path)
    {
        var music = Raylib.LoadMusicStream(path);
        musicStreams.Add(music);
        return music;
    }
    private static Font LoadFont(string path, int size)
    {
        var font = Raylib.LoadFontEx(path, size, 0);
        fonts.Add(font);
        return font;
    }
}
