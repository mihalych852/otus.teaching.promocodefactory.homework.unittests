using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    public class SetPartnerPromoCodeLimitMemoryDatabaseAsyncTests: IClassFixture<TestFixture_InMemory>
    {
        private readonly PartnersController _partnersController;
        private readonly IRepository<Partner> _partnersRepository;
        
        public SetPartnerPromoCodeLimitMemoryDatabaseAsyncTests(TestFixture_InMemory fixture)
        {
            var serviceProvider = fixture.ServiceProvider;

            _partnersRepository = serviceProvider.GetService<IRepository<Partner>>();
            _partnersController = new PartnersController(_partnersRepository);
        }


        //Если партнеру выставляется лимит, то мы должны обнулить количество промокодов,
        //которые партнер выдал NumberIssuedPromoCodes
        [Theory]
        [InlineData("7d994823-8226-4273-b063-1a95f3cc1df8")]
        public async Task If_Partner_Has_New_Limit_Number_Issued_PromoCodes_Should_Be_Null(string partnerId)
        {
            // Arange
            var limitRequest = BaseTests.GetTestPartnerPromoCodeLimitRequest(DateTime.Now, 20);
            var _partnerId = Guid.Parse(partnerId);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(_partnerId, limitRequest) as CreatedAtActionResult;
            var partner = await  _partnersRepository.GetByIdAsync(_partnerId);

            //Assert
            result.StatusCode.Should().Be(201);
            partner.NumberIssuedPromoCodes.Should().Be(0);
        }

        //если лимит закончился, то количество не обнуляется;
        [Theory]
        [InlineData("894b6e9b-eb5f-406c-aefa-8ccb35d39319")]
        public async Task If_Limit_Reached_Number_Issued_PromoCode_Should_Be_NotNull(string partnerId)
        {
            // Arange
            var limitRequest = BaseTests.GetTestPartnerPromoCodeLimitRequest(DateTime.Now, 20);
            var _partnerId = Guid.Parse(partnerId);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(_partnerId, limitRequest) as CreatedAtActionResult;
            var partner = await _partnersRepository.GetByIdAsync(_partnerId);

            //Assert
            result.StatusCode.Should().Be(201);
            partner.NumberIssuedPromoCodes.Should().Be(5);
        }


        //TODO
        [Theory]
        [InlineData("7d994823-8226-4273-b063-1a95f3cc1df8")]
        public async Task If_Limit_Was_Saved_In_Database(string partnerId)
        {
            // Arange
            var limit = 20;
            var date = DateTime.Now;
            var limitRequest = BaseTests.GetTestPartnerPromoCodeLimitRequest(date, limit);
            var _partnerId = Guid.Parse(partnerId);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(_partnerId, limitRequest) as CreatedAtActionResult;
            var partner = await _partnersRepository.GetByIdAsync(_partnerId);

            var newLimit = from l in partner.PartnerLimits
                           where l.Limit == limit && l.EndDate == date
                           select l;
            
            //Assert
            result.StatusCode.Should().Be(201);
            newLimit.Count().Should().Be(1);
        }

    }
}