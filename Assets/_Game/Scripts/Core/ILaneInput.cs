using System;

namespace Game.Core
{
    public interface ILaneInput
    {
        event Action<int> MoveRequested;
    }
}
