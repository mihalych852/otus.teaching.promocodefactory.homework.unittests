using Otus.Teaching.PromoCodeFactory.WebHost.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.Builders
{
    class PartnerPromoCodeLimitRequestBulider
    {
        private readonly SetPartnerPromoCodeLimitRequest _request;

        public PartnerPromoCodeLimitRequestBulider()
        {
            _request= new SetPartnerPromoCodeLimitRequest()
            {
                EndDate = DateTime.MaxValue,
                Limit = 4
            };
        }

        public PartnerPromoCodeLimitRequestBulider EndOn(DateTime endDate)
        {
            _request.EndDate = endDate;
            return this;
        }

        public PartnerPromoCodeLimitRequestBulider WithLimit(int limit)
        {
            _request.Limit = limit;
            return this;
        }

        public SetPartnerPromoCodeLimitRequest Build()
        {
            return _request;
        }
    }
}
