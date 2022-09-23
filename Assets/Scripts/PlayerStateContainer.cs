using System;
using Unity.Netcode;

[Serializable]
public class PlayerStateContainer : NetworkVariableBase
{
    public enum PlayerState
    {
        Alive = 0,
        Dying = 1,
        Dead = 2
    }

    public PlayerState _playerState;

    public override void WriteDelta(FastBufferWriter writer)
    {
    }

    public override void WriteField(FastBufferWriter writer)
    {
        writer.WriteValueSafe((int) _playerState);
    }

    public override void ReadField(FastBufferReader reader)
    {
        int tempPlayerState;
        reader.ReadValueSafe(out tempPlayerState);
        _playerState = (PlayerState) tempPlayerState;
    }

    public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
    {
    }
}