using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    public class SetPartnerPromoCodeLimitFixtureDatabaseAsyncTests
    {
        private readonly PartnersController _partnersController;
        private readonly Mock<IRepository<Partner>> _partnersRepository;

        private readonly Guid _partnerId;
        public SetPartnerPromoCodeLimitFixtureDatabaseAsyncTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
             _partnersRepository = fixture.Freeze<Mock<IRepository<Partner>>>();
             _partnersController = fixture.Build<PartnersController>().OmitAutoProperties().Create();

            _partnerId = Guid.NewGuid();

        }

   

        [Theory]
        [InlineData(0)]
        public async Task Limit_Should_Be_More_Than_Null(int limit)
        {
            // Arange
            var limitRequest = BaseTests.GetTestPartnerPromoCodeLimitRequest(DateTime.Now, limit);

            _partnersRepository.Setup(y => y.GetByIdAsync(_partnerId)).
                ReturnsAsync(() => BaseTests.CreateBasePartner(true));

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(_partnerId, limitRequest) as BadRequestObjectResult;

            // Assert
              Assert.Equal(result.StatusCode, 400);
            Assert.Equal(result.Value, "Лимит должен быть больше 0");
        }

       

    }
}