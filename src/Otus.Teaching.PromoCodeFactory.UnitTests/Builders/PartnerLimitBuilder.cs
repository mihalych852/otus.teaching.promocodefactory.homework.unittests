using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.Builders
{
    class PartnerLimitBuilder
    {
        private readonly PartnerPromoCodeLimit _partnerLimit;

        public PartnerLimitBuilder()
        {
            _partnerLimit = new PartnerPromoCodeLimit()
            {
                Id = Guid.Parse("91a2846f-a9fb-443f-b3bd-30e621ab7986"),
                CreateDate = DateTime.MinValue,
                EndDate = DateTime.MaxValue,
                CancelDate = null,
                Limit = 10,
            };

        }

        public PartnerLimitBuilder LimitedBy(int limit)
        {
            _partnerLimit.Limit = limit;
            return this;
        }

        public PartnerLimitBuilder PartnerId(Guid id)
        {
            _partnerLimit.PartnerId = id;
            return this;
        }

        public PartnerLimitBuilder CanceledAt(DateTime canceledAt)
        {
            _partnerLimit.CancelDate = canceledAt;
            return this;
        }

        public PartnerPromoCodeLimit Build()
        {
            return _partnerLimit;
        }



    }
}
