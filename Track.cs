using System.Text;

namespace SharpMania;

public sealed class Track
{
    public string? SourcePath { get; set; }
    public string Title { get; private set; }
    public string AudioPath { get; private set; }
    public float Offset { get; private set; }
    public List<(float, float)> Bpms { get; private set; }
    public List<TrackMap> Maps { get; private set; }

    private Track(string title, string audioPath, float offset, List<(float, float)> bpms, List<TrackMap> maps)
    {
        Title = title;
        AudioPath = audioPath;
        Offset = offset;
        Bpms = bpms;
        Maps = maps;
    }

    
    public static Track Parse(TextReader reader)
    {
        var track = new Track("Title not specified", "", 0, new(), new());
        while (true)
        {
            var line = reader.ReadLine();
            if (line == null) break;
            if (line.Length == 0) continue;
            if (line[0] == '/') continue;
            if (line[0] != '#') throw new Exception($"Unexpected line '{line}'");

            var tagEnd = line.IndexOf(':');
            var tag = line[1..tagEnd];

            if (tag == "NOTES")
            {
                track.Maps.Add(TrackMap.Parse(reader));
                continue;
            }

            var valueEnd = line.IndexOf(';');
            var value = line[(tagEnd + 1)..valueEnd];
            switch (tag)
            {
                case "TITLE":
                    track.Title = value;
                    break;
                case "MUSIC":
                    track.AudioPath = value;
                    break;
                case "OFFSET":
                    track.Offset = float.Parse(value);
                    break;
                case "BPMS":
                    TrackParserUtils.ParseCsv(track.Bpms, ',', value, s =>
                    {
                        var sep = s.IndexOf('=');
                        var left = float.Parse(s.AsSpan(0, sep));
                        var right = float.Parse(s.AsSpan(sep + 1, s.Length - (sep + 1)));
                        return (left, right);
                    });
                    break;
            }
        }
        track.Maps.Sort(new TrackMap.Comparer());
        return track;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Title: {Title}");
        builder.AppendLine($"Audio: {AudioPath}");
        builder.AppendLine($"Offset: {Offset}");
        builder.AppendLine($"BPMs: {string.Join(',', Bpms)}");
        foreach (var map in Maps)
        {
            var str = map.ToString();
            builder.AppendLine($"Map: \n{str}".Replace("\n", "\n    "));
        }
        return builder.ToString();
    }
}

public sealed class TrackMap
{
    public string Difficulty { get; private set; } = "None";
    public int Meter { get; private set; } = -1;
    public List<TrackMapNote> Notes { get; private set; } = new();
    
    public static TrackMap Parse(TextReader reader)
    {
        var map = new TrackMap();
        // Chart type
        reader.ReadLine();
        // Desc/author
        reader.ReadLine();
        // Difficulty
        map.Difficulty = reader.ReadLine()![3..^1];
        // Meter
        map.Meter = int.Parse(reader.ReadLine()!.AsSpan()[3..^1]);
        // Radar values
        reader.ReadLine();

        static void assignMeasureLength(List<TrackMapNote> notes, int measureLenght)
        {
            for (int i = 0; i < measureLenght; i++)
            {
                var note = notes[^(i+1)];
                note.MeasureLength = measureLenght;
                notes[^(i+1)] = note;
            }
        }

        var measure = 0;
        var measureLength = 0;
        while (true)
        {
            var line = reader.ReadLine();
            if (line == null) break;
            if (line.Length == 0) continue;
            if (line[0] == ';') break;
            if (line[0] == ',')
            {
                assignMeasureLength(map.Notes, measureLength);
                measure += 1;
                measureLength = 0;
                continue;
            }
            measureLength += 1;
            var note = new TrackMapNote()
            {
                Measure = measure,
                MeasureLength = -1,
                Tap = TrackParserUtils.ParseActionKey(line, '1'),
                HoldStart = TrackParserUtils.ParseActionKey(line, '2'),
                HoldEnd = TrackParserUtils.ParseActionKey(line, '3'),
            };
            map.Notes.Add(note);
        }
        assignMeasureLength(map.Notes, measureLength);
        return map;
    }
    
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Difficulty: {Difficulty}");
        builder.AppendLine($"Meter: {Meter}");
        foreach (var note in Notes)
        {
            builder.AppendLine($"{note.Measure} \t{note.MeasureLength} \t{note.Tap}");
        }
        return builder.ToString();
    }

    public class Comparer : IComparer<TrackMap>
    {
        public int Compare(TrackMap? x, TrackMap? y)
        {
            if (x == null) return -1;
            if (y == null) return 1;
            return x.Meter - y.Meter;
        }
    }
}

public struct TrackMapNote 
{
    public int Measure { get; set; }
    public int MeasureLength { get; set; }
    public ActionKey Tap { get; set; }
    public ActionKey HoldStart { get; set; }
    public ActionKey HoldEnd { get;  set; }
}

public static class TrackParserUtils
{
    public static List<T> ParseCsv<T>(char separator, string str, Func<string, T> elementParser)
    {
        var list = new List<T>();
        ParseCsv(list, separator, str, elementParser);
        return list;
    }

    public static void ParseCsv<T>(List<T> list, char separator, string str, Func<string, T> elementParser)
    {
        var i = 0;
        var prev = 0;
        while (true)
        {
            i = str.IndexOf(separator, i + 1);
            if (i == -1) break;
            list.Add(elementParser(str[prev..i]));
            prev = i + 1;
        }
        list.Add(elementParser(str[prev..]));
    }

    public static ActionKey ParseActionKey(ReadOnlySpan<char> line, char targetSymbol)
    {
        var a = ActionKey.None;
        if (line[0] == targetSymbol) a |= ActionKey.Left;
        if (line[1] == targetSymbol) a |= ActionKey.Down;
        if (line[2] == targetSymbol) a |= ActionKey.Up;
        if (line[3] == targetSymbol) a |= ActionKey.Right;
        return a;
    }
}
