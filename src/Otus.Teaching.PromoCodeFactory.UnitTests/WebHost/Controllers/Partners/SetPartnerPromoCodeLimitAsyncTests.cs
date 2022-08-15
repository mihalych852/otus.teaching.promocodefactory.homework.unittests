using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
using Otus.Teaching.PromoCodeFactory.WebHost.Models;
using Xunit;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{

    public class SetPartnerPromoCodeLimitAsyncTests
    {
        private readonly Mock<IRepository<Partner>> _partnersRepositoryMock;
        private readonly PartnersController _partnersController;
        private readonly Fixture _autoFixture;

        public SetPartnerPromoCodeLimitAsyncTests()
        {
            _autoFixture = new Fixture();
            _autoFixture.Customize(new AutoMoqCustomization());
            _partnersRepositoryMock = _autoFixture.Freeze<Mock<IRepository<Partner>>>();
            _partnersController = _autoFixture.Build<PartnersController>().OmitAutoProperties().Create();
        }

        public Partner CreateBasePartner()
        {
            var partner = new Partner()
            {
                Id = Guid.Parse("7d994823-8226-4273-b063-1a95f3cc1df8"),
                Name = "Суперигрушки",
                IsActive = true,
                PartnerLimits = new List<PartnerPromoCodeLimit>()
                {
                    new PartnerPromoCodeLimit()
                    {
                        Id = Guid.Parse("e00633a5-978a-420e-a7d6-3e1dab116393"),
                        CreateDate = new DateTime(2020, 07, 9),
                        EndDate = new DateTime(2020, 10, 9),
                        Limit = 100
                    }
                }
            };

            return partner;
        }

        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimitAsync_PartnerIsNotFound_ReturnsNotFound(SetPartnerPromoCodeLimitRequest setPartnerPromoCodeLimitRequest)
        {
            // Arrange
            var partnerId = Guid.Parse("def47943-7aaf-44a1-ae21-05aa4948b165");
            Partner partner = null;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, setPartnerPromoCodeLimitRequest);

            // Assert
            result.Should().BeAssignableTo<NotFoundResult>();
        }

        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimitAsync_PartnerIsNotActivated_ReturnsBadRequest(SetPartnerPromoCodeLimitRequest setPartnerPromoCodeLimitRequest)
        {
            // Arrange
            var partner = CreateBasePartner();
            partner.IsActive = false;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, setPartnerPromoCodeLimitRequest);

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }


        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimitAsync_LimitIsNotReached_ErasePromoCodes(SetPartnerPromoCodeLimitRequest setPartnerPromoCodeLimitRequest)
        {
            // Arrange
            var partner = CreateBasePartner();
            partner.NumberIssuedPromoCodes = _autoFixture.Create<int>();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, setPartnerPromoCodeLimitRequest);

            // Assert
            partner.NumberIssuedPromoCodes.Should().Be(0);
        }

        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimitAsync_LimitIsReached_PromoCodesAreNotErased(SetPartnerPromoCodeLimitRequest setPartnerPromoCodeLimitRequest)
        {
            // Arrange
            var partner = CreateBasePartner();
            var promoCodesNumber = _autoFixture.Create<int>();
            partner.NumberIssuedPromoCodes = promoCodesNumber;
            partner.PartnerLimits.First().CancelDate = DateTime.Now - TimeSpan.FromMinutes(10);

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, setPartnerPromoCodeLimitRequest);

            // Assert
            partner.NumberIssuedPromoCodes.Should().Be(promoCodesNumber);
        }

        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimitAsync_CancelPreviousLimit_Success(SetPartnerPromoCodeLimitRequest setPartnerPromoCodeLimitRequest)
        {
            // Arrange
            var initialTime = DateTime.Now;
            var partner = CreateBasePartner();
            var partnerLimit = partner.PartnerLimits.First();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, setPartnerPromoCodeLimitRequest);

            // Assert
            partnerLimit.CancelDate.GetValueOrDefault().Should().BeAfter(initialTime);
        }

        [Theory]
        [InlineData(-10)]
        [InlineData(0)]
        public async void SetPartnerPromoCodeLimitAsync_LimitEqualOrLessZero_BadRequest(int limit)
        {
            // Arrange
            var partner = CreateBasePartner();
            var setPartnerPromoCodeLimitRequest = _autoFixture.Create<SetPartnerPromoCodeLimitRequest>();
            setPartnerPromoCodeLimitRequest.Limit = limit;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, setPartnerPromoCodeLimitRequest);

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }

        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimitAsync_SaveNewLimit_Saved(SetPartnerPromoCodeLimitRequest setPartnerPromoCodeLimitRequest)
        {
            // Arrange
            var partner = CreateBasePartner();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            var initialLimitsCount = partner.PartnerLimits.Count();

            // Act
            await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, setPartnerPromoCodeLimitRequest);

            // Assert
            partner.PartnerLimits.Count.Should().Be(initialLimitsCount + 1);
        }
    }
}