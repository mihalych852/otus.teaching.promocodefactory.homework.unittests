using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;

using Moq;

using Xunit;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc;

using AutoFixture;
using Otus.Teaching.PromoCodeFactory.Core.Common;
using AutoFixture.AutoMoq;
using Otus.Teaching.PromoCodeFactory.UnitTests.Builders;
using FluentAssertions;
using System.Linq;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    public class SetPartnerPromoCodeLimitAsyncTests
    {
        //TODO: Add Unit Tests
        private PartnersController _partnerController;

        private Mock<IRepository<Partner>> _partnerRepositoryMock;
        private Mock<ICurrentDateTimeProvider> _currentDateTimeProviderMock;

        private readonly DateTime NowDate = new DateTime(2023, 04, 01);

        public SetPartnerPromoCodeLimitAsyncTests() 
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            _partnerRepositoryMock = fixture.Freeze<Mock<IRepository<Partner>>>();
            _currentDateTimeProviderMock = fixture.Freeze<Mock<ICurrentDateTimeProvider>>();
            {
                _currentDateTimeProviderMock.Setup(p => p.CurrentDateTime).Returns(NowDate);
            }

            _partnerController = fixture.Build<PartnersController>().OmitAutoProperties().Create();

        }


        // Варианты Id партнеров для использования в Тестах
        Guid partnerId_1 = Guid.Parse("def47943-7aaf-44a1-ae21-05aa4948b165");
        Guid partnerId_2 = Guid.Parse("7d994823-8226-4273-b063-1a95f3cc1df8");

        //1. Если партнер не найден, то также нужно выдать ошибку 404;
        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_PartnerIsNotFound_Return404()
        {

            //Arrange
            Partner partner = null;

            var request = new PartnerPromoCodeLimitRequestBulider().Build();

            _partnerRepositoryMock.Setup(p => p.GetByIdAsync(partnerId_1)).ReturnsAsync(partner);

            //Act
            var result = await _partnerController.SetPartnerPromoCodeLimitAsync(partnerId_1, request);
            //Assert
            result.Should().BeAssignableTo<NotFoundResult>();

        }

        //2. Если партнер заблокирован, то есть поле IsActive=false в классе Partner, то также нужно выдать ошибку 400.
        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_PartnersNotActive_Return400()
        {

            //Arrange
            var partner = new PartnerBuilder(partnerId_2).IsActive(false).Build();

            var request = new PartnerPromoCodeLimitRequestBulider().Build();

            _partnerRepositoryMock.Setup(p => p.GetByIdAsync(partnerId_2)).ReturnsAsync(partner);

            //Act
            var result = await _partnerController.SetPartnerPromoCodeLimitAsync(partnerId_2, request);
            //Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();


        }

        /// 3. Если партнеру выставляется лимит, то мы должны обнулить количество промокодов, которые партнер выдал NumberIssuedPromoCodes.
        /// если лимит закончился, то количество не обнуляется.
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_PartnerHasActiveLimit_ResetIssuedPromoCodes()
        {
            
            //Arrange
            var partner = new PartnerBuilder(partnerId_2).Build();

            var request = new PartnerPromoCodeLimitRequestBulider().Build();

            _partnerRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id)).ReturnsAsync(partner);

            //Act
            var result = await _partnerController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            //Assert
            result.Should().BeAssignableTo<CreatedAtActionResult>();
            partner.NumberIssuedPromoCodes.Should().Be(0);

        }

        //3. Если партнеру выставляется лимит, то мы должны обнулить количество промокодов, которые партнер выдал NumberIssuedPromoCodes.
        // если лимит закончился, то количество не обнуляется.

        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_PartnerHasNoActiveLimit_NotResetIssuedPromocodes()
        {
            //Arrange
            var partner = new PartnerBuilder(partnerId_2).WithEmptyPromoCodeLimits().WithPromocodesCount(1).Build();

            var request = new PartnerPromoCodeLimitRequestBulider().Build();
            
            _partnerRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id)).ReturnsAsync(partner);

            //Act
            var result = await _partnerController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            //Assert
            result.Should().BeAssignableTo<CreatedAtActionResult>();

            partner.NumberIssuedPromoCodes.Should().Be(1);
        }

        // 4. При установке лимита нужно отключить предыдущий лимит.
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_SetLimit_ResetPreviousLimit()
        {
            //Arrange
            var request = new PartnerPromoCodeLimitRequestBulider().Build();


            var partner = new PartnerBuilder(partnerId_2).Build();

            var partnerLimit = partner.PartnerLimits.First();

            _partnerRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id)).ReturnsAsync(partner);

            //Act
            var result = await _partnerController.SetPartnerPromoCodeLimitAsync(partnerId_2, request);

            //Assert
            result.Should().BeAssignableTo<CreatedAtActionResult>();
            partnerLimit.CancelDate.Should().Be(NowDate.Date);

        }

        //5. Лимит должен быть больше 0.
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_LimitNegative_ReturnBadRequest()
        {

            //Arrange
            var request = new PartnerPromoCodeLimitRequestBulider().WithLimit(-1).Build();

            var partner = new PartnerBuilder(partnerId_2).Build();

            _partnerRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id)).ReturnsAsync(partner);

            //Act
            var result = await _partnerController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            //Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();


        }
        /// <summary>
        /// 6. Нужно убедиться, что сохранили новый лимит в базу данных (это нужно проверить Unit-тестом).
        /// </summary>

        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_SetLimit_AddNewLimitIntoDb()
        {
            //Arrange
            var request = new PartnerPromoCodeLimitRequestBulider().Build();

            var partner = new PartnerBuilder(partnerId_2).Build();

            var partnerLimit = partner.PartnerLimits.First();

            _partnerRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id)).ReturnsAsync(partner);

            // Act
            var result = await _partnerController.SetPartnerPromoCodeLimitAsync(partner.Id, request);


            //Assert
            result.Should().BeAssignableTo<CreatedAtActionResult>();

            _partnerRepositoryMock.Verify(p => p.UpdateAsync(partner), Times.Once);

        }
    }
}