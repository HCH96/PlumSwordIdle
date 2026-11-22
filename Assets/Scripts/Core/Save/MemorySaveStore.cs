using System;

namespace PlumSword.Core.Save
{
    public sealed class MemorySaveStore : ISaveStore
    {
        public string Text { get; private set; }

        public bool TryDelete() { Text = null; return true; }

        public bool TryWrite(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            Text = text;
            return true;
        }
    }
}
