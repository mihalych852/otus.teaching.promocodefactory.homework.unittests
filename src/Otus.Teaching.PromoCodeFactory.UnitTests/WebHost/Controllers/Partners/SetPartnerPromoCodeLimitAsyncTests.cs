using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;

using Moq;

using Xunit;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc;


namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    public class SetPartnerPromoCodeLimitAsyncTests
    {
        //TODO: Add Unit Tests
        private PartnersController _partnerController;
        private Mock<IRepository<Partner>> _partnerRepositoryMock = new Mock<IRepository<Partner>>();

        public SetPartnerPromoCodeLimitAsyncTests() 
        { 
            _partnerController = new PartnersController(_partnerRepositoryMock.Object);

        }

        //1. Если партнер не найден, то также нужно выдать ошибку 404;
        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_If_Partner_IsNotFound_Returns_StatusCode_404()
        {

            //Arrange
            var partnerId = Guid.Parse("def47943-7aaf-44a1-ae21-05aa4948b165");
            Partner partner = null;

            _partnerRepositoryMock.Setup(p => p.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(partner);

            //Act
            var expect = typeof(NotFoundResult);
            var actual = await _partnerController.SetPartnerPromoCodeLimitAsync(partnerId, null);

            //Assert
            Assert.IsType(expect, actual);
        }



    }
}