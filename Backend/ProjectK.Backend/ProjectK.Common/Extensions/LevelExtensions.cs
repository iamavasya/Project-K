using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Extensions
{
    public static class LevelExtensions
    {
        public static string ToDisplayName(this PlastLevel level) => level switch
        {
            //PlastLevel.Neimenovanyi => "Неіменований",
            //PlastLevel.Pryhylnyk => "Прихильник",
            //PlastLevel.Uchasnyk => "Учасник",
            //PlastLevel.Skob => "Скоб",
            //PlastLevel.HetmanskyiSkob => "Гетьманський скоб",
            //PlastLevel.StarshoplastunPryhylnyk => "Старшопластун прихильник",
            //PlastLevel.Starshoplastun => "Старшопластун",
            //PlastLevel.StarshoplastunSkob => "Старшопластун скоб",
            //PlastLevel.StarshoplastunHetmanskyiSkob => "Старшопластун гетьманський скоб",
            //PlastLevel.SeniorPryhylnyk => "Сеньйор прихильник",
            //PlastLevel.SeniorPratsi => "Сеньйор праці",
            //PlastLevel.SeniorDoviria => "Сеньйор довір'я",
            //PlastLevel.SeniorKerivnytstva => "Сеньйор керівництва",
            //_ => level.ToString()
        };
    }
}
