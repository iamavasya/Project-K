---
title: Міграції EF Core
description: Як створити, застосувати і відкотити міграцію бази.
sidebar:
  order: 4
---

Команди виконуються з теки `Backend/ProjectK.Backend`. Проєкт із міграціями — `ProjectK.Infrastructure`,
стартовий — `ProjectK.API`.

**Створити міграцію**

```powershell
dotnet ef migrations add <MigrationName> --startup-project ProjectK.API --project ProjectK.Infrastructure
```

**Застосувати до бази**

```powershell
dotnet ef database update --startup-project ProjectK.API --project ProjectK.Infrastructure
```

**Прибрати останню (ще не застосовану)**

```powershell
dotnet ef migrations remove --startup-project ProjectK.API --project ProjectK.Infrastructure
```

:::note
При локальному запуску бекенд застосовує міграції автоматично на старті, тож окремий
`database update` зазвичай не потрібен. Ручне застосування корисне для інспекції або в CI.
:::

## Правила

- Міграція іменується за тим, що змінює в домені, а не за датою.
- Числа enum у базі не переставляються (див. [Конвенції](/dev/core/contributing/#числа-enum-що-зберігаються--заморожені)).
- Прибирання колонок після великого рефакторингу збирається в одну міграцію «після релізу», а не
  розмазується по дрібних: так self-host-інсталяції оновлюються одним кроком
  (див. [Оновлення self-host](/dev/self-host/update/)).
- Перед міграцією на проді — бекап (див. [Backup і restore](/dev/self-host/backup-restore/)).
