using System;
public interface IKillable
{
    event Action<IKillable> OnKilled;
}