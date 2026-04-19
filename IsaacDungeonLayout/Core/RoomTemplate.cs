namespace IsaacDungeonLayout;

/// <summary>Шаблон комнаты: направления выходов в локальной системе шаблона (до поворота).</summary>
public sealed class RoomTemplate
{
    public required string Id { get; init; }
    public required RoomType Type { get; init; }
    public required int OutsNum { get; init; }
    public required IReadOnlyList<Int2> OutsDir { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new InvalidOperationException("RoomTemplate.Id пустой.");
        if (OutsNum != OutsDir.Count)
            throw new InvalidOperationException($"Шаблон {Id}: OutsNum ({OutsNum}) != OutsDir.Count ({OutsDir.Count}).");
        foreach (var d in OutsDir)
        {
            int m = Math.Abs(d.X) + Math.Abs(d.Z);
            if (m != 1)
                throw new InvalidOperationException($"Шаблон {Id}: направление {d} не кардинальное единичное.");
        }

        var set = new HashSet<Int2>(OutsDir);
        if (set.Count != OutsDir.Count)
            throw new InvalidOperationException($"Шаблон {Id}: дубли направлений в OutsDir.");

        switch (Type)
        {
            case RoomType.Base when OutsNum is < 2 or > 4:
                throw new InvalidOperationException($"Шаблон {Id}: base должен иметь 2..4 выхода.");
            case RoomType.Mob or RoomType.Start or RoomType.End or RoomType.Plug when OutsNum != 1:
                throw new InvalidOperationException($"Шаблон {Id}: тип {Type} требует ровно 1 выход.");
        }
    }
}
