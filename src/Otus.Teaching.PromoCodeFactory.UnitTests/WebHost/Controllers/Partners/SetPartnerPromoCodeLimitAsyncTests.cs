using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using FluentAssertions.Common;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using Otus.Teaching.PromoCodeFactory.UnitTests.Helpers;
using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
using Otus.Teaching.PromoCodeFactory.WebHost.Models;
using Xunit;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    public class SetPartnerPromoCodeLimitAsyncTests
    {
        private readonly PartnersController _partnersController;
        private readonly Mock<IRepository<Partner>> _partnersRepositoryMock;
        private readonly IFixture _fixture;
        private readonly TestDataGenerator _dataGenerator;

        public SetPartnerPromoCodeLimitAsyncTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            // Из за рекурсии при создании моделей, указываем о необходимости прекращения рекурсии
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            // Благодоря Freeze в "виртуальном" IoC создаем единственный инстанс репозитория
            _partnersRepositoryMock = _fixture.Freeze<Mock<IRepository<Partner>>>();
            _partnersController = _fixture.Build<PartnersController>().OmitAutoProperties().Create();

            _dataGenerator = new TestDataGenerator(_fixture);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        public async void SetPartnerPromoCodeLimitAsync_PartnerIsNotFoundOrUnactive_ReturnsNotFound(bool? activeStatus)
        {
            // Arrange
            var partnerId = _dataGenerator.GetRandomUuid();

            Partner partner = activeStatus != null
                ? _dataGenerator.GetPartner(activeStatus)
                : null;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId,
                _dataGenerator.GetSetPartnerPromoCodeLimitRequest());

            // Assert
            if (activeStatus == null)
                result.Should().BeAssignableTo<NotFoundResult>();
            if(activeStatus != null)
                result.Should().BeAssignableTo<BadRequestObjectResult>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("10.10.2022")]
        public async void SetPartnerPromoCodeLimitAsync_BehaviorLimit_ReturnsNotFound(string? cancelLimitStringDate)
        {
            // Arrange
            DateTime? cancelLimitDate;

            cancelLimitDate = cancelLimitStringDate != null
                ? DateTime.ParseExact(cancelLimitStringDate, "dd.MM.yyyy", CultureInfo.InvariantCulture)
                : null;

            var partnerId = _dataGenerator.GetRandomUuid();

            var partner = _dataGenerator.GetPartner
            (true,
                new List<PartnerPromoCodeLimit>() { _dataGenerator.GetPartnerPromoCodeLimit(cancelLimitDate) });

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            var request = _dataGenerator.GetSetPartnerPromoCodeLimitRequest();

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, request);

            // Assert
            var expectedNumberIssuedPromoCodes = cancelLimitStringDate == null
                ? 0
                : partner.NumberIssuedPromoCodes;

            var expectedCancelDateLimit = cancelLimitStringDate == null
                ? DateTime.Now
                : partner.PartnerLimits.FirstOrDefault().CancelDate;
            
            result.Should().BeAssignableTo<CreatedAtActionResult>();
            
            // Проверка на установку значения NumberIssuedPromoCodes
            _partnersRepositoryMock.Verify(x =>
                x.UpdateAsync(It.Is<Partner>(x =>
                    x.NumberIssuedPromoCodes.IsSameOrEqualTo(expectedNumberIssuedPromoCodes))), Times.Once);
            
            // Проверка на установку значения CancelDate
            _partnersRepositoryMock.Verify(x =>
                x.UpdateAsync(It.Is<Partner>(x =>
                    x.PartnerLimits.Any(x => x.CancelDate.Value.Date.IsSameOrEqualTo(expectedCancelDateLimit.Value.Date)))), Times.Once);

            // Проверка на запись в БД
            /*_partnersRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Partner>(x =>
                        x.PartnerLimits.Count == partner.PartnerLimits.Count + 1)));*/
        }
    }
}