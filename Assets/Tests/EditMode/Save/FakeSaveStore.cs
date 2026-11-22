using System;
using PlumSword.Core.Save;

namespace PlumSword.Core.Tests.Save
{
    public sealed class FakeSaveStore : ISaveStore
    {
        public int WriteCount { get; private set; }
        public string LastText { get; private set; }
        public bool Succeeds { get; set; } = true;
        public bool Throws { get; set; }
        public bool TryDelete() { LastText = null; return true; }

        public bool TryWrite(string text)
        {
            WriteCount++;
            LastText = text;
            if (Throws) throw new InvalidOperationException();
            return Succeeds;
        }
    }
}
