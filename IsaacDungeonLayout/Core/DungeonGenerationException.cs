namespace IsaacDungeonLayout;

/// <summary>Явный сбой генерации после исчерпания попыток или нарушения конфигурации.</summary>
public sealed class DungeonGenerationException : InvalidOperationException
{
    public DungeonGenerationException(string message, int attemptsUsed)
        : base(message)
    {
        AttemptsUsed = attemptsUsed;
    }

    public int AttemptsUsed { get; }
}
