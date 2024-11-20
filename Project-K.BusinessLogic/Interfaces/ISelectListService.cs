using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Project_K.BusinessLogic.Interfaces
{
    public interface ISelectListService
    {
        SelectList GetSelectList(string listName);
    }
}
