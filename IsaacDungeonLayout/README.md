# IsaacDungeonLayout

C#-библиотека генерации подземелий (сетка, шаблоны, валидация): режим **с нуля** (`Generate`), режим **shuffle** на готовом графе, **метаданные** комнат (BFS от старта/финиша/мобов).

| Документ | Назначение |
|----------|------------|
| **[Documentation.md](Documentation.md)** | Полная документация: режимы, архитектура, валидация, риски |
| **[docs/GettingStarted.md](docs/GettingStarted.md)** | Сборка, `dotnet run`, первые примеры, ProjectReference |
| **[GodotIntegration.md](GodotIntegration.md)** | Godot 4 C#: сцены, спавн, метаданные, shuffle |
| [docs/ShuffleMode.md](docs/ShuffleMode.md) | Контракт shuffle, `Seed`, API |
| [docs/RoomGameplayMetadata.md](docs/RoomGameplayMetadata.md) | Поля DTO, ключи `SetMeta` |

```bash
cd IsaacDungeonLayout
dotnet build
dotnet run -- test
```
