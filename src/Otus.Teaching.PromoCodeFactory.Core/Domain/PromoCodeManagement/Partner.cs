using System;
using System.Collections.Generic;
using System.Linq;

namespace Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement
{
    public class Partner
        : BaseEntity
    {
        public string Name { get; set; }

        public int NumberIssuedPromoCodes  { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<PartnerPromoCodeLimit> PartnerLimits { get; set; }

        public PartnerPromoCodeLimit ActiveLimit => PartnerLimits.FirstOrDefault(x => !x.CancelDate.HasValue);
    }
}