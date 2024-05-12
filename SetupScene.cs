using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Toolkit.HighPerformance;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Raylib_CsLo;

using SharpMania.OSBindings;

namespace SharpMania;

public sealed class SetupScene : Scene
{
    private sbyte[] trackPath = new sbyte[256];
    private bool editingTrackPath = false;
    private Track? track = null;
    private string trackMapsStringList = "";
    private bool editingTrackMap = false;
    private int trackMapIndex = 0;
    private GameOptions options = new();

    public override void Load()
    {
        LoadConfig();
    }

    public override void Unload()
    {
    }

    public override void Update()
    {
    }

    public override void Draw()
    {
        RayGui.GuiLabel(new(10, 10, 100, 12), "Track (.sm file)");
        if (RayGui.GuiButton(new(Screen.width - 60, 8, 50, 12), "Browse"))
        {
            ShowTrackPathDialog();
        }
        unsafe
        {
            fixed (sbyte* ptr = trackPath)
            {
                if (RayGui.GuiTextBox(new(10, 22, Screen.width - 20, 12), ptr, trackPath.Length - 1, editingTrackPath))
                {
                    if (editingTrackPath)
                    {
                        LoadTrack(Encoding.UTF8.GetString(trackPath.AsSpan(0, Array.IndexOf<sbyte>(trackPath, 0)).AsBytes()));
                    }
                    editingTrackPath = !editingTrackPath;
                }
            }
        }
        if (track == null)
        {
            Raylib.DrawTextEx(GameResources.lazyFox1Font, "Track is not loaded", new(10, 36), 11, 1, Colors.Red);
        }
        else
        {
            RayGui.GuiLabel(new(10, 36, 100, 12), track.Title);
            unsafe
            {
                var mapIndex = trackMapIndex;
                if (RayGui.GuiDropdownBox(new(290, 36, 100, 12), trackMapsStringList, &mapIndex, editingTrackMap))
                {
                    editingTrackMap = !editingTrackMap;
                }
                trackMapIndex = mapIndex;
            }
        }

        RayGui.GuiLabel(new(10, 60, 100, 12), "Volume");
        options.masterVolume = RayGui.GuiSliderBar(new(100, 60, 100, 12), "0%", "100%", options.masterVolume, 0f, 1f);

        RayGui.GuiLabel(new(10, 74, 100, 12), "Arrows Speed");
        options.notesScrollSpeed = RayGui.GuiSliderBar(new(100, 74, 100, 12), "min", "max", options.notesScrollSpeed, 50f, 500f);
        RayGui.GuiLabel(new(104, 74, 100, 12), $"{options.notesScrollSpeed}");

        options.useAssistSound = RayGui.GuiCheckBox(new(10, 88, 12, 12), "Assist Sound", options.useAssistSound);

        RayGui.GuiLabel(new(10, 140, 100, 60), "Controls:\nWASD\nFGJK\nArrows");
        
        if (track == null) RayGui.GuiDisable();
        if (RayGui.GuiButton(new(290, 210, 100, 24), "Play"))
        {
            if (track == null) return;
            SceneMediator.EnterTrackScene(track, track.Maps[trackMapIndex], options);
        }
        RayGui.GuiEnable();
    }

    private void LoadConfig()
    {
        try
        {
            using StreamReader reader = File.OpenText(@"config.json");
            JObject o = (JObject)JToken.ReadFrom(new JsonTextReader(reader));

            LoadTrack(o["DefaultTrack"]?.Value<string>() ?? "");
        }
        catch (Exception ex)
        {
            Raylib.TraceLog(TraceLogLevel.LOG_WARNING, $"Exception while loading config: {ex.Message}");
        }
    }

    private void ShowTrackPathDialog()
    {
        var ofn = new OpenFileName()
        {
            filter = "Stepmania tracks (.sm)\0*.sm\0",
            file = new string(new char[256]),
            maxFile = 255,
            title = "Select Track File",
        };
        ofn.structSize = Marshal.SizeOf(ofn);

        if (WinApi.GetOpenFileNameW(ref ofn) && ofn.file != null)
        {
            LoadTrack(ofn.file);
        }
    }

    private bool LoadTrack(string path)
    {
        track = null;
        if (!File.Exists(path)) return false;
        var extension = Path.GetExtension(path);
        if (extension != ".sm") return false;

        SetTrackPath(path);
        try
        {
            using var reader = File.OpenText(path);
            track = Track.Parse(reader);
            track.SourcePath = path;
            trackMapsStringList = string.Join(';', track.Maps.Select(m => $"{m.Meter} {m.Difficulty}"));
            if (trackMapIndex >= track.Maps.Count) trackMapIndex = track.Maps.Count - 1;
        }
        catch (Exception ex)
        {
            Raylib.TraceLog(TraceLogLevel.LOG_ERROR, $"Exception while loading track: {ex.Message}");
            return false;
        }
        return true;
    }

    private void SetTrackPath(string path)
    {
        Array.Clear(trackPath);
        Encoding.UTF8.GetBytes(path, trackPath.AsSpan().AsBytes());
    }
}
