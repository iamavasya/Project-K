# Оновлення self-host-інсталяції

Для інсталяцій, поставлених із Docker self-host bundle релізу.

## Перед оновленням

1. Прочитай нотатки до релізу.
2. Зроби [копії бази і файлів](backup-restore.md).
3. Збережи поточний `.env`.
4. Перевір, що томи на місці:

```bash
docker volume ls --filter name=projectk
```

Дані живуть в іменованих томах Docker; оновлення образів їх не чіпає.

Ніколи не виконуй це, якщо не хочеш видалити всі дані інсталяції:

```bash
docker compose down -v
```

## Оновлення в тій самій теці bundle

У `.env` постав цільову версію:

```text
PROJECTK_VERSION=<версія>
```

Витягни нові образи з GHCR і перезапусти:

```bash
docker compose pull
docker compose up -d
```

API застосовує міграції EF Core на старті. Великі рефакторинги (як 0.20.0 з переходом на
членства) роблять це довше за звичайне — дай API хвилину і перевір `docker compose logs -f projectk-api`.

## Теги образів

Compose тягне образи за `PROJECTK_VERSION`:

```text
ghcr.io/iamavasya/projectk-api:${PROJECTK_VERSION}
ghcr.io/iamavasya/projectk-web:${PROJECTK_VERSION}
```

Для живих інсталяцій — явна версія (наприклад `1.0.0`). Тег `beta` рухається до найновішої бети і
годиться лише тим, хто свідомо хоче йти за нею.

## Оновлення з нового bundle

1. Завантаж новий `projectk-<версія>-docker-selfhost`.
2. Розпакуй у нову теку.
3. Скопіюй туди свій `.env`.
4. Переглянь `.env.example` — чи не зʼявились нові змінні.
5. Виконай:

```bash
docker compose pull
docker compose up -d
```

Bundle використовує явні назви томів `projectk-sql-data` і `projectk-azurite-data`, тож дані
лишаються привʼязаними до інсталяції навіть при зміні назви теки.

## Якщо інсталяція старіша за 0.14.2-beta

Ранні bundle давали томам назви з префіксом теки, наприклад
`projectk-0.14.1-beta-docker-selfhost_projectk-sql-data`. Перед запуском нової версії з нової теки
перевір реальні назви томів (`docker volume ls --filter name=projectk`). Якщо дані в томах із
префіксом — зроби копію зі старої теки і віднови її в нові томи `projectk-sql-data` та
`projectk-azurite-data`.

## Відкат

Відкат безпечний лише коли схема бази сумісна з попередньою версією. Після невдалої міграції чи
великих змін — відновлюйся з копії, а не відкочуй образ.
