using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
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
        private readonly IFixture _fixture;

        public SetPartnerPromoCodeLimitAsyncTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _partnersRepositoryMock = _fixture.Freeze<Mock<IRepository<Partner>>>();
            _partnersController = _fixture.Build<PartnersController>().OmitAutoProperties().Create();

            _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            _fixture.Customizations.Add(new RandomNumericSequenceGenerator(10, 30));
            _fixture.Customize<Partner>(c => c.With(p => p.IsActive, true));
        }

        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_PartnerIsNotFound_ReturnsNotFound()
        {
            // Arrange
            var partnerId = _fixture.Create<Guid>();
            Partner partner = null;

            var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, request);

            // Assert
            result.Should().BeAssignableTo<NotFoundResult>();
        }

        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_PartnerIsBlocked_ReturnsBadRequest()
        {
            // Arrange
            var partner = _fixture.Build<Partner>().With(p => p.IsActive, false).Create();

            var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }

        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_SetNewLimitWithAlreadyActiveLimit_ClearNumberIssuedPromoCodes()
        {
            // Arrange
            var activePromoCodeLimit = _fixture
                .Build<PartnerPromoCodeLimit>().Without(p => p.CancelDate).Create();

            var partner = _fixture
                .Build<Partner>()
                .With(p => p.PartnerLimits, new List<PartnerPromoCodeLimit> {activePromoCodeLimit}).Create();

            var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            partner.NumberIssuedPromoCodes.Should().Be(0);
        }

        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_SetNewLimitWithoutActiveLimit_NotChangeNumberIssuedPromoCodes()
        {
            // Arrange
            var partner = _fixture.Create<Partner>();
            var expectedNumberIssuedPromoCodes = partner.NumberIssuedPromoCodes;

            var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            partner.NumberIssuedPromoCodes.Should().Be(expectedNumberIssuedPromoCodes);
        }

        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_SetNewLimitWithAlreadyActiveLimit_CancelPreviousPromoCode()
        {
            // Arrange
            var activePromoCodeLimit = _fixture
                .Build<PartnerPromoCodeLimit>().Without(p => p.CancelDate).Create();

            var partner = _fixture
                .Build<Partner>()
                .With(p => p.PartnerLimits, new List<PartnerPromoCodeLimit> { activePromoCodeLimit }).Create();

            var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            activePromoCodeLimit.CancelDate.Should().NotBeNull();
        }

        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_SetNegativeLimits_ReturnsBadRequestsOnNegativeLimits()
        {
            // Arrange
            _fixture.Customizations.Insert(0, new RandomNumericSequenceGenerator(int.MinValue, -1));

            var partner = _fixture.Create<Partner>();
            var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }

        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_SetZeroLimits_ReturnsBadRequestsOnZeroLimits()
        {
            // Arrange
            var partner = _fixture.Create<Partner>();
            var request = _fixture.Build<SetPartnerPromoCodeLimitRequest>().With(r => r.Limit, 0).Create();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }

        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_SetPositiveLimits_SuccessfulCreatedNewLimit()
        {
            // Arrange
            _fixture.Customizations.Insert(0, new RandomNumericSequenceGenerator(1, int.MaxValue));

            var partner = _fixture.Create<Partner>();
            var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            result.Should().BeAssignableTo<CreatedAtActionResult>();
        }

        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_SetValidLimit_LimitAddedToRepository()
        {
            // Arrange
            var partner = _fixture.Create<Partner>();
            var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            partner.PartnerLimits.Should().Contain(x => x.Limit == request.Limit && x.EndDate == request.EndDate);
            _partnersRepositoryMock.Verify(x => x.UpdateAsync(partner));
        }
    }
}