namespace IsaacDungeonLayout;

/// <summary>Ожидаемые числа Base/Mob для валидации shuffle-layout (Start/End по одному).</summary>
public readonly record struct ShuffleTypeExpectation(int BaseCount, int MobCount)
{
    public static ShuffleTypeExpectation FromSlots(IReadOnlyList<RoomSlotDescriptor> slots)
    {
        int b = 0, m = 0;
        foreach (var s in slots)
        {
            switch (s.RoomType)
            {
                case RoomType.Base: b++; break;
                case RoomType.Mob: m++; break;
            }
        }

        return new ShuffleTypeExpectation(b, m);
    }
}

/// <summary>Вход режима перестановки: фиксированный граф клеток, старт и пул слотов (биекция слот↔клетка).</summary>
public sealed class ShuffleDungeonInput
{
    public required HashSet<Int2> OccupiedCells { get; init; }
    public required Int2 StartPosition { get; init; }
    public required IReadOnlyList<RoomSlotDescriptor> Slots { get; init; }
    public required IReadOnlyList<RoomTemplate> Templates { get; init; }
    /// <summary>Детерминированный tie-break порядка перебора (кандидаты End, клетки, слоты в backtracking).</summary>
    public int Seed { get; init; }
    public Action<string>? DiagnosticLog { get; init; }

    public void Validate()
    {
        if (OccupiedCells.Count == 0)
            throw new InvalidOperationException("OccupiedCells пуст.");
        if (!OccupiedCells.Contains(StartPosition))
            throw new InvalidOperationException("StartPosition должна входить в OccupiedCells.");
        if (Slots.Count != OccupiedCells.Count)
            throw new InvalidOperationException($"Число слотов ({Slots.Count}) должно совпадать с числом клеток ({OccupiedCells.Count}).");

        int s = 0, e = 0;
        foreach (var sl in Slots)
        {
            if (sl.RoomType == RoomType.Start) s++;
            else if (sl.RoomType == RoomType.End) e++;
        }

        if (s != 1 || e != 1)
            throw new InvalidOperationException($"В слотах должно быть ровно один Start и один End (сейчас Start={s}, End={e}).");

        if (Templates.Count == 0)
            throw new InvalidOperationException("Нужен хотя бы один RoomTemplate.");

        foreach (var t in Templates)
            t.Validate();

        if (!GridBfs.IsConnected(OccupiedCells))
            throw new InvalidOperationException("Граф клеток OccupiedCells должен быть связным по 4-соседству.");

        if (!DegreeMultisetMatchesGrid(this, out var degErr))
            throw new InvalidOperationException(degErr ?? "Несовпадение степеней слотов и сетки.");

        static bool DegreeMultisetMatchesGrid(ShuffleDungeonInput input, out string? error)
        {
            int slotsDeg1 = 0, slotsBase = 0;
            foreach (var sl in input.Slots)
            {
                if (sl.RoomType == RoomType.Base)
                    slotsBase++;
                else
                    slotsDeg1++;
            }

            int cellsDeg1 = 0, cellsBase = 0;
            foreach (var p in input.OccupiedCells)
            {
                int g = GridBfs.CellDegree(p, input.OccupiedCells);
                if (g == 1)
                    cellsDeg1++;
                else if (g is >= 2 and <= 4)
                    cellsBase++;
                else
                {
                    error = $"Клетка {p}: степень сетки {g} не поддерживается (ожидается 1 для S/E/Mob или 2..4 для Base).";
                    return false;
                }
            }

            if (slotsDeg1 != cellsDeg1 || slotsBase != cellsBase)
            {
                error =
                    $"Несовпадение степеней: слотов с типом не-Base (S/E/Mob)={slotsDeg1}, клеток степени 1={cellsDeg1}; Base-слотов={slotsBase}, клеток степени 2..4={cellsBase}.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
