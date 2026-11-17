namespace PlumSword.Core.Save
{
    public enum SaveLoadStatus
    {
        Empty = 0,
        Loaded = 1,
        Corrupt = 2,
        UnsupportedVersion = 3
    }
}
