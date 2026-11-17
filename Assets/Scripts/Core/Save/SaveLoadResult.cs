using System;

namespace PlumSword.Core.Save
{
    public sealed class SaveLoadResult
    {
        public SaveLoadResult(SaveLoadStatus status, SaveData data)
        {
            if ((status == SaveLoadStatus.Loaded) != (data != null))
                throw new ArgumentException("Data must be present exactly when status is Loaded.", nameof(data));
            Status = status;
            Data = data;
        }

        public SaveLoadStatus Status { get; }
        public SaveData Data { get; }
        public bool HasSaveData => Status == SaveLoadStatus.Loaded;
    }
}
