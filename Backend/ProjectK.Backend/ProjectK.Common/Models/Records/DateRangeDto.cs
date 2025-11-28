using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Records;

public readonly record struct DateRangeDto(DateTime Start, DateTime End);
