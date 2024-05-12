using System.Numerics;
using System.Text;

using Raylib_CsLo;

namespace SharpMania;

public struct RenderedNote
{
    public int index;
    public int localIndex;
    public int beatPartition;
    public TrackMapNote note;
    public float arrivalTime;
    public ActionKey tapHit;
    public bool passedTarget;
    public bool passedJudgement;
}

public struct ScoreMark
{
    public float windowSeconds;
    public int score;
    public string name;
    public bool breakCombo;
    public Rectangle spriteRect;

    public ScoreMark(float windowSeconds, int score, string name, bool breakCombo, Rectangle spriteRect)
    {
        this.windowSeconds = windowSeconds;
        this.score = score;
        this.name = name;
        this.breakCombo = breakCombo;
        this.spriteRect = spriteRect;
    }
}

public struct TempSprite
{
    public float spawnTime;
    public float duration;
    public Rectangle spriteRect;
    public int salt; // random number for visuals
}

public sealed class TrackScene : Scene
{
    private static ScoreMark missScoreMark = new(0, 0, "Miss", true, new Rectangle(0, 16, 80, 16));
    private static ScoreMark[] scoreMarks = new ScoreMark[]
    {
        new(.160f, 10, "Meh", true, new Rectangle(0, 48, 96, 16)),
        new(.100f, 30, "Ok", false, new Rectangle(0, 80, 96, 16)),
        new(.060f, 50, "Good", false, new Rectangle(0, 112, 96, 16)),
        new(.040f, 80, "Nice", false, new Rectangle(0, 146, 96, 16)),
        new(.017f, 100, "Perfect", false, new Rectangle(0, 178, 96, 16)),
    };
    private static int[] beatPartitions = new int[] { 4, 8, 12, 16 };

    private Music trackAudio;

    private Track track;
    private TrackMap map;
    private GameOptions options;
    private float musicTimePlayed = 0f;
    private float timeSinceTrackStart = 0f;
    private float beatsPerSecond = 1f;
    private int bpmIndex = 0;
    private float currentBeat = 0f;
    private int currentNoteIndex = 0;
    private int currentMeasure = 0;
    private int currentMeasureStart = 0;
    private float currentNoteTime = 0f;
    private int score = 0;
    private int maxScore = 0;
    private int comboCounter = 0;
    private int maxComboCounter = 0;
    private ActionKey pressedKeys = ActionKey.None;
    private ActionKey downKeys = ActionKey.None;
    private ActionKey shouldBeHoldedKeys = ActionKey.None;
    private bool isEnded = false;
    private float endTime;
    private float bpsTime;
    private Random rng = new();

    private CustomQueue<RenderedNote> activeNotes = new();
    private CustomQueue<TempSprite> scoreMarksVisuals = new();

    public TrackScene(Track track, TrackMap map, GameOptions options)
    {
        this.track = track;
        this.map = map;
        this.options = options;

        beatsPerSecond = track.Bpms[0].Item2 / 60f;
    }

    public override void Load()
    {
        LoadTrackAudio();
        Raylib.SetMasterVolume(options.masterVolume);
        Raylib.SetMusicVolume(trackAudio, 0.1f);
        Raylib.PlayMusicStream(trackAudio);
    }

    public override void Unload()
    {
        Raylib.UnloadMusicStream(trackAudio);
    }

    private void LoadTrackAudio()
    {
        //if (track == null) throw new NullReferenceException("Track should be loaded first");
        if (track.SourcePath == null) throw new NullReferenceException("Track source path should be defined");
        var fullSourceTrackPath = Path.GetFullPath(track.SourcePath, AppDomain.CurrentDomain.BaseDirectory);
        var fullPath = Path.GetFullPath(track.AudioPath, Path.GetDirectoryName(fullSourceTrackPath) ?? "");
        trackAudio = Raylib.LoadMusicStream(fullPath);
    }

    private void SetMap(TrackMap map)
    {
        this.map = map;
        //var tapsCount = 0;
        //for (int i = 0; i < trackMap.Notes.Count; i++)
        //{
        //    var note = trackMap.Notes[i];
        //    tapsCount += CountKeys(note.Tap);
        //}
        //maxScore = tapsCount * scoreMarks[^1].score;
    }

    public override void Update()
    {
        timeSinceTrackStart += Raylib.GetFrameTime();

        Raylib.UpdateMusicStream(trackAudio);
        
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_MINUS))
        {
            options.masterVolume = Math.Clamp(options.masterVolume - 0.1f, 0f, 1f);
            Raylib.SetMasterVolume(options.masterVolume);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_EQUAL))
        {
            options.masterVolume = Math.Clamp(options.masterVolume + 0.1f, 0f, 1f);
            Raylib.SetMasterVolume(options.masterVolume);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_NINE))
        {
            Raylib.SeekMusicStream(trackAudio, 0.9f * Raylib.GetMusicTimeLength(trackAudio));
        }

        if (!isEnded && Raylib.GetMusicTimeLength(trackAudio) - Raylib.GetMusicTimePlayed(trackAudio) < 0.1f)
        {
            FinishTrack();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
        {
            SceneMediator.EnterSetupScene();
            return;
        }

        musicTimePlayed = Raylib.GetMusicTimePlayed(trackAudio) + track.Offset;

        UpdateKeysInput();
        UpdateNotes();
    }

    private void UpdateKeysInput()
    {
        void updateBinding(KeyboardKey keyboardKey, ActionKey actionKey)
        {
            if (Raylib.IsKeyPressed(keyboardKey))
                pressedKeys |= actionKey;
            if (Raylib.IsKeyDown(keyboardKey))
                downKeys |= actionKey;
        }

        pressedKeys = ActionKey.None;
        downKeys = ActionKey.None;

        // WASD
        updateBinding(KeyboardKey.KEY_A, ActionKey.Left);
        updateBinding(KeyboardKey.KEY_S, ActionKey.Down);
        updateBinding(KeyboardKey.KEY_W, ActionKey.Up);
        updateBinding(KeyboardKey.KEY_D, ActionKey.Right);

        // FGJK
        updateBinding(KeyboardKey.KEY_F, ActionKey.Left);
        updateBinding(KeyboardKey.KEY_G, ActionKey.Down);
        updateBinding(KeyboardKey.KEY_J, ActionKey.Up);
        updateBinding(KeyboardKey.KEY_K, ActionKey.Right);

        // Arrows
        updateBinding(KeyboardKey.KEY_LEFT, ActionKey.Left);
        updateBinding(KeyboardKey.KEY_DOWN, ActionKey.Down);
        updateBinding(KeyboardKey.KEY_UP, ActionKey.Up);
        updateBinding(KeyboardKey.KEY_RIGHT, ActionKey.Right);
    }

    private void UpdateNotes()
    {
        var forwardLookupSeconds = 3f;
        var dropSeconds = -1f;
        var nextNoteIndex = currentNoteIndex + 1;
        if (nextNoteIndex < map.Notes.Count)
        {
            var nextNote = map.Notes[nextNoteIndex];
            var nextNoteMeasure = currentMeasure;
            var nextNoteMeasureStart = currentMeasureStart;
            if (nextNote.Measure > nextNoteMeasure)
            {
                nextNoteMeasure = nextNote.Measure;
                nextNoteMeasureStart = nextNoteIndex;
            }
            var nextNoteLocalIndex = nextNoteIndex - nextNoteMeasureStart;
            var nextNoteBeat = (nextNoteMeasure * 4) + nextNoteLocalIndex / (float)nextNote.MeasureLength * 4f;
            var nextNoteTime = currentNoteTime + (nextNoteBeat - currentBeat) / beatsPerSecond;
            if (nextNoteTime - forwardLookupSeconds <= musicTimePlayed)
            {
                currentNoteIndex = nextNoteIndex;
                currentMeasure = nextNoteMeasure;
                currentMeasureStart = nextNoteMeasureStart;
                currentNoteTime = nextNoteTime;
                currentBeat = nextNoteBeat;
                if ((nextNote.Tap | nextNote.HoldStart | nextNote.HoldEnd) != ActionKey.None)
                {
                    var measureLength = nextNote.MeasureLength;
                    var beatPartition = beatPartitions.Length - 1;
                    for (int bp = 0; bp < beatPartitions.Length; bp++)
                    {
                        var val = beatPartitions[bp];
                        if (measureLength % val != 0) continue;
                        var corrected = measureLength / val;
                        if (nextNoteLocalIndex % corrected == 0)
                        {
                            beatPartition = bp;
                            break;
                        }
                    }
                    var renderedNote = new RenderedNote()
                    {
                        note = nextNote,
                        arrivalTime = nextNoteTime,
                        index = currentNoteIndex,
                        localIndex = nextNoteLocalIndex,
                        beatPartition = beatPartition,
                        tapHit = ActionKey.None,
                    };
                    activeNotes.Enqueue(renderedNote);
                }
                if (bpmIndex + 1 < track.Bpms.Count && track.Bpms[bpmIndex + 1].Item1 <= currentBeat)
                {
                    bpmIndex++;
                    beatsPerSecond = track.Bpms[bpmIndex].Item2 / 60f;
                }
            }
        }
        var wasHitTap = ActionKey.None;
        for (var i = 0; i < activeNotes.Count; i++)
        {
            var note = activeNotes[i];
            if (!note.passedTarget && note.arrivalTime <= musicTimePlayed)
            {
                note.passedTarget = true;
                activeNotes[i] = note;
                if (options.useAssistSound) 
                {
                    Raylib.PlaySound(GameResources.assistSound);
                }
                var successHold = note.note.HoldEnd & shouldBeHoldedKeys;
                if (successHold != ActionKey.None)
                {
                    note.tapHit |= successHold;
                    activeNotes[i] = note;
                    shouldBeHoldedKeys &= ~successHold;
                    var count = successHold.Count();
                    ApplyScore(scoreMarks[^1], count);
                }
            }
            var distSecondsSigned = note.arrivalTime - musicTimePlayed;
            var distSeconds = MathF.Abs(distSecondsSigned);
            if (distSeconds <= scoreMarks[0].windowSeconds)
            {
                var ableToPress = (~note.tapHit) & (~wasHitTap) & pressedKeys;
                var tap = note.note.Tap & ableToPress;
                var holdStart = note.note.HoldStart & ableToPress;
                var releasedHold = shouldBeHoldedKeys & ~downKeys;
                note.tapHit |= tap | holdStart | releasedHold;
                wasHitTap |= tap | holdStart;
                activeNotes[i] = note;
                shouldBeHoldedKeys &= ~releasedHold;
                shouldBeHoldedKeys |= holdStart;
                var count = (tap | holdStart | releasedHold).Count();
                if (count > 0)
                {
                    var markIndex = 0;
                    while (markIndex + 1 < scoreMarks.Length && distSeconds <= scoreMarks[markIndex + 1].windowSeconds)
                    {
                        markIndex += 1;
                    }
                    var mark = scoreMarks[markIndex];
                    ApplyScore(mark, count);
                }
            }
            else if (distSecondsSigned < scoreMarks[0].windowSeconds && !note.passedJudgement)
            {
                note.passedJudgement = true;
                activeNotes[i] = note;
                var missedTaps = note.note.Tap & (~note.tapHit);
                var count = missedTaps.Count();
                if (count > 0)
                {
                    ApplyScore(missScoreMark, count);
                }
            }
        }        

        var missHoldedKeys = shouldBeHoldedKeys & ~downKeys;
        if (missHoldedKeys != ActionKey.None)
        {
            shouldBeHoldedKeys &= ~missHoldedKeys;
            var count = missHoldedKeys.Count();
            ApplyScore(missScoreMark, count);
        }

        if (activeNotes.TryPeek(out var topNote) && topNote.arrivalTime - musicTimePlayed < dropSeconds)
        {
            activeNotes.Dequeue();
        }
    }

    private void ApplyScore(ScoreMark mark, int count)
    {
        score += count * mark.score;
        maxScore += count * scoreMarks[^1].score;
        comboCounter = mark.breakCombo ? 0 : comboCounter + count;
        if (comboCounter > maxComboCounter) maxComboCounter = comboCounter;
        if (!mark.breakCombo) 
        {
            Raylib.PlaySound(GameResources.shotSound);
            //Raylib.SetSoundPitch(GameResources.shotSound, 0.9f + rng.NextSingle() * 0.2f);
        }
        for (int j = 0; j < count; j++)
        {
            scoreMarksVisuals.Enqueue(new TempSprite()
            {
                spawnTime = timeSinceTrackStart,
                duration = 1f,
                spriteRect = mark.spriteRect,
                salt = j,
            });
        }
    }

    private void FinishTrack()
    {
        isEnded = true;
        endTime = timeSinceTrackStart;
        Raylib.PauseMusicStream(trackAudio);
    }

    public override void Draw()
    {
        //Raylib.DrawRectangle(0, 0, Screen.width, Screen.height,
        //    Colors.FromHSV(bpsTime * 360f * 0.2f, 0.3f, 0.9f));
        Raylib.DrawRectangle(0, 0, Screen.width, Screen.height, new Color(72, 70, 76, 255));
        bpsTime += beatsPerSecond * Raylib.GetFrameTime();

        //DrawTrackDebug();

        DrawVolumeBar();
        DrawJudgementArrows();
        DrawArrows();
        DrawScoreMarks();

        Raylib.DrawText(score.ToString("D8"), 320, 5, 16, Colors.White);
        Raylib.DrawText((maxScore == 0 ? 1f : score / (float)maxScore).ToString("P2"), 320, 25, 9, Colors.White);
        Raylib.DrawText(comboCounter.ToString(), 11, 205, 32, Colors.White);

        if (isEnded)
        {
            Raylib.DrawRectangle(0, 0, Screen.width, Screen.height, 
                new Color(72, 70, 76, (int)(255 * MathExt.TimeProgress(endTime, timeSinceTrackStart, 1f))));

            Raylib.DrawText(track.Title, 150, 20, 11, Colors.White);
            Raylib.DrawText($"{map.Meter} {map.Difficulty}", 150, 34, 11, Colors.White);
            Raylib.DrawText(score.ToString("D8"), 150, 100, 20, Colors.White);
            Raylib.DrawText((maxScore == 0 ? 1f : score / (float)maxScore).ToString("P2"), 150, 124, 11, Colors.White);
            Raylib.DrawText($"Max Combo: {maxComboCounter}", 150, 150, 11, Colors.White);
        }
    }


    private void DrawVolumeBar()
    {
        var pos = new Vector2(340, 220);
        var size = new Vector2(50, 4);
        var filledSize = size * new Vector2(options.masterVolume, 1f);
        Raylib.DrawRectangleV(pos, size, Colors.LightGray);
        Raylib.DrawRectangleV(pos, filledSize, Colors.Black);
    }

    private void DrawArrows()
    {
        var notesScrollSpeed = options.notesScrollSpeed;
        const int arrowSize = 32;
        const int arrowMargin = 4;
        float getRowPos(int row) => 200 + arrowSize * (-2 + row) + arrowMargin * (-1.5f + row);
        float getRowCenter(int row) => 200 + arrowSize * (-2 + row) + arrowMargin * (-1.5f + row) + arrowSize;
        void drawArrow(float y, int row, RenderedNote note, bool isDisabled)
        {
            var variation = isDisabled ? 4 : note.beatPartition;
            Raylib.DrawTexturePro(
                GameResources.arrowTex,
                new Rectangle(32 * row, variation * 32, 32, 32),
                new Rectangle(getRowPos(row), y, arrowSize, arrowSize),
                new Vector2(0.5f, 0.5f),
                0,
                Colors.White);
        }
        float getArrowPos(RenderedNote note)
        {
            var noteTimeOffset = note.arrivalTime - musicTimePlayed;
            return 16 + noteTimeOffset * notesScrollSpeed;
        }
        float getArrowCenter(RenderedNote note) => getArrowPos(note) + arrowSize / 2;
        void drawNoteForKey(RenderedNote note, ActionKey key, int row, ref RenderedNote? holdStartNote)
        {
            var holdStart = note.note.HoldStart;
            var holdEnd = note.note.HoldEnd;
            var yPos = getArrowPos(note);
            if (holdStart.HasFlag(key))
            {
                holdStartNote = note;
            }
            else if (holdEnd.HasFlag(key))
            {
                var rowPos = getRowPos(row);
                var endPos = getArrowCenter(note);
                var isDisabled = false;
                if (holdStartNote.HasValue)
                {
                    drawTopHoldArrowWithLine(holdStartNote.Value, key, row, endPos, out isDisabled);
                }
                else
                {
                    var isHolded = shouldBeHoldedKeys.HasFlag(key);
                    var startPos = isHolded ? 16 + arrowSize / 2 : 0;
                    isDisabled = !isHolded;
                    drawHoldLine(rowPos, startPos, endPos, isDisabled);
                }
                drawArrow(yPos, row, note, isDisabled);
                holdStartNote = null;
            }
            else
                drawArrow(yPos, row, note, false);
        }
        void drawHoldLine(float x, float start, float end, bool isDisabled)
        {
            if (end <= start) return;
            x += arrowSize / 2;
            Raylib.DrawLineEx(new(x, start), new(x, end), 16f, isDisabled ? Colors.LightGray : Colors.Red);
        }
        void drawTopHoldArrowWithLine(RenderedNote startNote, ActionKey key, int row, float endPos, out bool isDisabled)
        {
            var rowPos = getRowPos(row);
            var startPos = getArrowCenter(startNote);
            isDisabled = startNote.passedJudgement && !startNote.tapHit.HasFlag(key);
            drawHoldLine(rowPos, startPos, endPos, isDisabled);
            drawArrow(getArrowPos(startNote), row, startNote, isDisabled);
        }

        RenderedNote? leftHoldStart = null;
        RenderedNote? downHoldStart = null;
        RenderedNote? upHoldStart = null;
        RenderedNote? rightHoldStart = null;
        foreach (var note in activeNotes)
        {
            var tap = note.note.Tap;
            var holdStart = note.note.HoldStart;
            var holdEnd = note.note.HoldEnd;
            var unhitTap = (tap | holdStart | holdEnd) & ~note.tapHit;
            if (unhitTap.HasFlag(ActionKey.Left))
                drawNoteForKey(note, ActionKey.Left, 0, ref leftHoldStart);
            if (unhitTap.HasFlag(ActionKey.Down))
                drawNoteForKey(note, ActionKey.Down, 1, ref downHoldStart);
            if (unhitTap.HasFlag(ActionKey.Up))
                drawNoteForKey(note, ActionKey.Up, 2, ref upHoldStart);
            if (unhitTap.HasFlag(ActionKey.Right))
                drawNoteForKey(note, ActionKey.Right, 3, ref rightHoldStart);
        }
        if (leftHoldStart.HasValue)
        {
            drawTopHoldArrowWithLine(leftHoldStart.Value, ActionKey.Left, 0, Screen.height, out _);
        }
        if (downHoldStart.HasValue)
        {
            drawTopHoldArrowWithLine(downHoldStart.Value, ActionKey.Down, 1, Screen.height, out _);
        }
        if (upHoldStart.HasValue)
        {
            drawTopHoldArrowWithLine(upHoldStart.Value, ActionKey.Up, 2, Screen.height, out _);
        }
        if (rightHoldStart.HasValue)
        {
            drawTopHoldArrowWithLine(rightHoldStart.Value, ActionKey.Right, 3, Screen.height, out _);
        }
    }

    private void DrawJudgementArrows()
    {
        void DrawArrow(int index, ActionKey targetKey)
        {
            const int arrowSize = 32;
            const int arrowMargin = 4;
            Raylib.DrawTexturePro(
                GameResources.judgementArrowTex,
                new Rectangle(32 * index, downKeys.HasFlag(targetKey) ? 32 : 0, 32, 32),
                new Rectangle(200 + arrowSize * (-2 + index) + arrowMargin * (-1.5f + index), 16, arrowSize, arrowSize),
                new Vector2(0.5f, 0.5f),
                0,
                Raylib.WHITE);
        }

        DrawArrow(0, ActionKey.Left);
        DrawArrow(1, ActionKey.Down);
        DrawArrow(2, ActionKey.Up);
        DrawArrow(3, ActionKey.Right);
    }

    private void DrawScoreMarks()
    {
        var posA = new Rectangle(152, 4, 96, 16);
        var posB = new Rectangle(152, -16, 96, 16);
        for (int i = 0; i < scoreMarksVisuals.Count; i++)
        {
            var m = scoreMarksVisuals[i];
            var random = MathExt.Hash((m.spawnTime, m.salt).GetHashCode());
            var angle = ((random % 100) / 50f - 1) * 5f;
            var lifetime = timeSinceTrackStart - m.spawnTime;
            var lifetimeFraction = lifetime / m.duration;
            var pos = posA.Lerp(posB, Ease.InCubic(lifetimeFraction));
            Raylib.DrawTexturePro(GameResources.scoreMarksTex, m.spriteRect, pos, new Vector2(0.5f, 0.5f), angle, Colors.White);
        }
        if (scoreMarksVisuals.TryPeek(out var topMark) && topMark.spawnTime + topMark.duration <= timeSinceTrackStart)
        {
            scoreMarksVisuals.Dequeue();
        }
    }
    
    private void DrawTrackDebug()
    {
        var builder = new StringBuilder();
        builder.Append($"{Raylib.GetFPS()} FPS\n");
        builder.Append($"Time since start: {timeSinceTrackStart}\n");
        builder.Append($"Music time: {musicTimePlayed}\n");
        builder.Append($"BPM: {beatsPerSecond * 60}\n");
        builder.Append($"Beat: {currentBeat}\n");
        builder.Append($"Active Notes: {activeNotes.Count}\n");
        Raylib.DrawText(builder.ToString(), 5, 5, 6, Colors.LightGray);
    }

}
