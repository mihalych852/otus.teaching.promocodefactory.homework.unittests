using AutoFixture.AutoMoq;
using AutoFixture;
using Moq;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
using Otus.Teaching.PromoCodeFactory.WebHost.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Namotion.Reflection;
using System.Linq;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    public class SetPartnerPromoCodeLimitAsyncTests
    {
        private readonly Mock<IRepository<Partner>> _partnersRepositoryMock;
        private readonly PartnersController _partnersController;
        private readonly SetPartnerPromoCodeLimitRequest _request;

        public SetPartnerPromoCodeLimitAsyncTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _partnersRepositoryMock = fixture.Freeze<Mock<IRepository<Partner>>>();
            _partnersController = fixture.Build<PartnersController>().OmitAutoProperties().Create();
            _request = fixture.Build<SetPartnerPromoCodeLimitRequest>().OmitAutoProperties().Create();
        }

        #region 1) Если партнер не найден, то также нужно выдать ошибку 404
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_PartnerIsNotFound_ReturnsNotFound()
        {
            // Arrange
            var partnerId = Guid.Parse("def47943-7aaf-44a1-ae21-05aa4948b165");
            Partner partner = null;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, _request);

            // Assert
            result.Should().BeAssignableTo<NotFoundResult>();
        }
        #endregion

        #region 2) Если партнер заблокирован, то есть поле IsActive=false в классе Partner, то также нужно выдать ошибку 400
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_PartnerIsNotActive_BadRequest()
        {
            // Arrange
            var partner = TestHelper.CreateBasePartner();
            partner.IsActive = false;

            var partnerId = partner.Id;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, _request);

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>()
                .Which.Value.Should().Be("Данный партнер не активен");
        }
        #endregion

        #region 3) Если партнеру выставляется лимит, то мы должны обнулить количество промокодов, которые партнер выдал NumberIssuedPromoCodes
        //, если лимит закончился, то количество не обнуляется;
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_SetActiveLimit_NumberIssuedPromoCodesEqualsZero()
        {
            // Arrange
            var partner = TestHelper.CreateBasePartner();
            var partnerId = partner.Id;
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, _request);

            // Assert
            partner.NumberIssuedPromoCodes.Should().Be(0);
        }
        #endregion

        #region 4) При установке лимита нужно отключить предыдущий лимит
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_SetActiveLimit_ActiveLimitCancelDateHasValue()
        {
            // Arrange
            var partner = TestHelper.CreateBasePartner();
            var partnerId = partner.Id;
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            var activeLimit = partner.PartnerLimits.FirstOrDefault(x =>
                !x.CancelDate.HasValue);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, _request);

            // Assert
            activeLimit.CancelDate.Should().NotBeNull();
        }
        #endregion

        #region 5) Лимит должен быть больше 0
        [Theory]
        [InlineData(-100)]
        [InlineData(0)]
        public async void SetPartnerPromoCodeLimitAsync_NegativeLimite_BadRequest(int a)
        {
            // Arrange
            var partner = TestHelper.CreateBasePartner();
            var partnerId = partner.Id;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            _request.Limit = a;

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, _request);

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>()
                .Which.Value.Should().Be("Лимит должен быть больше 0");
        }
        #endregion

        #region 6) Нужно убедиться, что сохранили новый лимит в базу данных (это нужно проверить Unit-тестом)
        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public async void SetPartnerPromoCodeLimitAsync_ConditionыPassedSuccess_PartnerPromoCodeLimitCreteAndSave(int a)
        {
            // Arrange
            var partner = TestHelper.CreateBasePartner();
            var partnerId = partner.Id;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            _request.Limit = a;

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, _request);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            partner.PartnerLimits.Should()
                .Contain(x => x.Limit == a 
                        && x.EndDate == _request.EndDate 
                        && x.PartnerId == partner.Id);
            _partnersRepositoryMock
                .Verify(repo => repo.UpdateAsync(It.IsAny<Partner>()), Times.Once);
        }
        #endregion
    }
}