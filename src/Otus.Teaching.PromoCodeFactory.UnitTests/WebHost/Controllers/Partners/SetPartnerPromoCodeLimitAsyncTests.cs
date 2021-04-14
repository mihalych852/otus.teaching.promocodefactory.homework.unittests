using Microsoft.AspNetCore.Mvc;
using Moq;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
using Otus.Teaching.PromoCodeFactory.WebHost.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    public class SetPartnerPromoCodeLimitAsyncTests
    {
        private readonly PartnersController _partnersController;
        private readonly Mock<IRepository<Partner>> _partnersRepositoryMock;

        public SetPartnerPromoCodeLimitAsyncTests()
        {
            _partnersRepositoryMock = new Mock<IRepository<Partner>>();
            _partnersController = new PartnersController(_partnersRepositoryMock.Object);
        }

        [Fact]
        public async Task If_Partner_Not_Found_Should_Return_404Error()
        {
            // Arange
            var limitRequest = BaseTests.GetTestPartnerPromoCodeLimitRequest(DateTime.Now);
            
            _partnersRepositoryMock.Setup(y => y.GetByIdAsync(It.IsAny<Guid>())).
                ReturnsAsync(() => null);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(Guid.NewGuid(), limitRequest) as NotFoundResult;

            // Assert
            Assert.Equal(result.StatusCode, 404);
        }

        [Fact]
        public async Task If_Partner_Blocked_Should_Return_400Error()
        {   
            // Arange
            var partnerId = Guid.NewGuid();
            var limitRequest = BaseTests.GetTestPartnerPromoCodeLimitRequest(DateTime.Now);

            _partnersRepositoryMock.Setup(y => y.GetByIdAsync(partnerId)).
                ReturnsAsync(() => BaseTests.CreateBasePartner(false));

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, limitRequest) as BadRequestObjectResult;

            // Assert
            Assert.Equal(result.StatusCode, 400);

        }


      
    }
}