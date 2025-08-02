using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Entities
{
    public abstract class Entity
    {
        protected Entity()
        {
            CreatedDate = UpdatedDate = DateTime.UtcNow;
        }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime UpdatedDate { get; set; }
    }
}
