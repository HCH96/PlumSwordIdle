namespace PlumSword.Core.Save
{
    public interface ISaveStore
    {
        bool TryWrite(string text);
    }
}
