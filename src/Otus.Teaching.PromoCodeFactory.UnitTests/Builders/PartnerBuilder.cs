using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using System;
using System.Collections.Generic;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.Builders
{
    public class PartnerBuilder
    {
        private readonly Partner _partner;
        public PartnerBuilder(Guid guid) 
        {
            _partner = new Partner()
            {
                Id = guid,
                Name = "СуперЗверушки",
                IsActive = true,
                NumberIssuedPromoCodes = 20,
                PartnerLimits = new List<PartnerPromoCodeLimit>()
                {
                    new PartnerPromoCodeLimit()
                    {
                        Id = Guid.Parse("e00633a5-978a-420e-a7d6-3e1dab116393"),
                        CreateDate = new DateTime(2022, 07, 9),
                        EndDate = new DateTime(2023, 10, 9),
                        Limit = 100,
                    }
                }
            };
        }

        public PartnerBuilder IsActive(bool isActive = true)
        {
            _partner.IsActive = isActive;
            return this;
        }

        public PartnerBuilder WithName(string name)
        {
            _partner.Name = name;
            return this;
        }

        public PartnerBuilder WithEmptyPromoCodeLimits()
        {
            _partner.PartnerLimits = new List<PartnerPromoCodeLimit>();
            return this;
        }

        public PartnerBuilder WithPromocodesCount(int numberIssuedPromocodes)
        {
            _partner.NumberIssuedPromoCodes = numberIssuedPromocodes;
            return this;
        }

        public Partner Build()
        {
            return _partner;
        }
    }
}
