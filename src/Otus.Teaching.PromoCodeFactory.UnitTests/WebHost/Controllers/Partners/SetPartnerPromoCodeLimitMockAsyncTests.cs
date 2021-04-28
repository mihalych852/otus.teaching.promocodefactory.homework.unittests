using FluentAssertions;
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
    public class SetPartnerPromoCodeLimitMockAsyncTests
    {
        private readonly PartnersController _partnersController;
        private readonly Mock<IRepository<Partner>> _partnersRepositoryMock;

        public SetPartnerPromoCodeLimitMockAsyncTests()
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
            var actionResult = await _partnersController.SetPartnerPromoCodeLimitAsync(Guid.NewGuid(), limitRequest) as NotFoundResult;
            var result = actionResult as NotFoundResult;

            // Assert
            actionResult.Should().BeAssignableTo<NotFoundResult>();
            result.StatusCode.Should().Be(404);
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
            var actionResult = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, limitRequest);
            var result = actionResult as BadRequestObjectResult;

            // Assert
            actionResult.Should().BeAssignableTo<BadRequestObjectResult>();
            result.StatusCode.Should().Be(400);

        }

    }
}