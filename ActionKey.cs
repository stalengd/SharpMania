namespace SharpMania;

[Flags]
public enum ActionKey
{
    None    = 0,
    Left    = 0b0001,
    Down    = 0b0010,
    Up      = 0b0100,
    Right   = 0b1000,
};

public static class ActionKeyExtensions
{
    public static int Count(this ActionKey key)
    {
        var val = (int)key;
        int count = 0;
        count += val >> 0 & 1;
        count += val >> 1 & 1;
        count += val >> 2 & 1;
        count += val >> 3 & 1;
        //count += val >> 4 & 1;
        //count += val >> 5 & 1;
        //count += val >> 6 & 1;
        //count += val >> 7 & 1;
        return count;
    }
}
