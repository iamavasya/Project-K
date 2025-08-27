using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Services.OrphanCleanup
{
    public sealed class OrphanCleanupOptions
    {
        // Увімкнути/вимкнути фоновий сервіс.
        public bool Enabled { get; init; } = true;

        // Інтервал між повними проходами (за замовчуванням 6 год).
        public TimeSpan Interval { get; init; } = TimeSpan.FromHours(6);

        // Мінімальний "вік" blob перед видаленням (щоб не стерти щойно завантажене).
        public TimeSpan GracePeriod { get; init; } = TimeSpan.FromHours(1);

        // Ліміт видалень за один прохід (захист від масового стирання).
        public int MaxDeletesPerRun { get; init; } = 500;

        // Якщо true – лише лог, без фактичного видалення.
        public bool DryRun { get; init; } = false;

        // Додаткова хаотична затримка (джиттер) у секундах (0..JitterSeconds).
        public int JitterSeconds { get; init; } = 30;
    }
}
