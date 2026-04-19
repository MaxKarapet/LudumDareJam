namespace IsaacDungeonLayout;

/// <summary>Нарушение инвариантов уже сгенерированного layout (не путать с <see cref="DungeonGenerationException"/>).</summary>
public sealed class DungeonLayoutValidationException : InvalidOperationException
{
    public DungeonLayoutValidationException(string message)
        : base(message)
    {
    }
}
