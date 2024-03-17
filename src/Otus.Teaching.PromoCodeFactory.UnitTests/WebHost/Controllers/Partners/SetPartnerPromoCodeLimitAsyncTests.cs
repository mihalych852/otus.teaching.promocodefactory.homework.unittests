using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
using Otus.Teaching.PromoCodeFactory.WebHost.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    public class SetPartnerPromoCodeLimitAsyncTests
    {
        private Mock<IRepository<Partner>> _repositoryMock;
        private Fixture _fixture;

        public SetPartnerPromoCodeLimitAsyncTests()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(_ => _fixture.Behaviors.Remove(_));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            _repositoryMock = new Mock<IRepository<Partner>>();
        }

        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_ShouldReturnsNotFound_WhenPartnerNotExists()
        {
            // Arrange.

            var requestFixture = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            _repositoryMock.Setup(_ => _.GetByIdAsync(It.IsAny<Guid>())).Returns(async () => null);

            var partnersController = new PartnersController(_repositoryMock.Object);

            // Act.
            
            var result = await partnersController.SetPartnerPromoCodeLimitAsync(Guid.NewGuid(), requestFixture);

            // Assert.

            result.Should().BeOfType(typeof(NotFoundResult));
        }

        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_ShouldReturnsBadRequest_WhenPartnerIsNotActive()
        {
            // Arrange.

            var requestFixture = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            var partnerFixture = _fixture.Build<Partner>()
                                        .With(_ => _.IsActive, false)
                                        .Create();

            _repositoryMock.Setup(_ => _.GetByIdAsync(It.IsAny<Guid>())).Returns(async () => partnerFixture);

            var partnersController = new PartnersController(_repositoryMock.Object);

            // Act.

            var result = await partnersController.SetPartnerPromoCodeLimitAsync(Guid.NewGuid(), requestFixture);

            // Assert.

            result.Should().BeOfType(typeof(BadRequestObjectResult));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(8)]
        [InlineData(15)]
        [InlineData(100)]
        public async Task SetPartnerPromoCodeLimitAsync_ShouldResetNumberIssuedPromoCodesAndCancelDate_WhenPartnerHasActiveLimits(int numberIssuedPromoCodes)
        {
            // Arrange.

            var requestFixture = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            var activeLimitFixture = _fixture.Build<PartnerPromoCodeLimit>()
                                            .Without(_ => _.CancelDate)
                                            .Create();

            var partnerFixture = _fixture.Build<Partner>()
                                        .With(_ => _.IsActive, true)
                                        .With(_ => _.NumberIssuedPromoCodes, numberIssuedPromoCodes)
                                        .With(_ => _.PartnerLimits, 
                                            new List<PartnerPromoCodeLimit> { activeLimitFixture })
                                        .Create();

            _repositoryMock.Setup(_ => _.GetByIdAsync(It.IsAny<Guid>())).Returns(async () => partnerFixture);

            var partnersController = new PartnersController(_repositoryMock.Object);

            // Act.

            await partnersController.SetPartnerPromoCodeLimitAsync(Guid.NewGuid(), requestFixture);

            // Assert.

            partnerFixture.NumberIssuedPromoCodes.Should().Be(0);
            activeLimitFixture.CancelDate.Should().NotBeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(8)]
        [InlineData(15)]
        [InlineData(100)]
        public async Task SetPartnerPromoCodeLimitAsync_ShouldNotResetNumberIssuedPromoCodes_WhenPartnerHasNoActiveLimits(int numberIssuedPromoCodes)
        {
            // Arrange.

            var requestFixture = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            var partnerFixture = _fixture.Build<Partner>()
                                        .With(_ => _.IsActive, true)
                                        .With(_ => _.NumberIssuedPromoCodes, numberIssuedPromoCodes)
                                        .With(_ => _.PartnerLimits, new List<PartnerPromoCodeLimit>())
                                        .Create();

            _repositoryMock.Setup(_ => _.GetByIdAsync(It.IsAny<Guid>())).Returns(async () => partnerFixture);

            var partnersController = new PartnersController(_repositoryMock.Object);

            // Act.

            await partnersController.SetPartnerPromoCodeLimitAsync(Guid.NewGuid(), requestFixture);

            // Assert.

            partnerFixture.NumberIssuedPromoCodes.Should().Be(numberIssuedPromoCodes);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        [InlineData(-123)]
        public async Task SetPartnerPromoCodeLimitAsync_ShouldReturnsBadRequest_WhenRequestLimitLessOrEqualsZero(int limit)
        {
            // Arrange.

            var requestFixture = _fixture.Build<SetPartnerPromoCodeLimitRequest>()
                                        .With(_ => _.Limit, limit)
                                        .Create();

            var partnerFixture = _fixture.Build<Partner>()
                                        .With(_ => _.IsActive, true)
                                        .Create();

            _repositoryMock.Setup(_ => _.GetByIdAsync(It.IsAny<Guid>())).Returns(async () => partnerFixture);

            var partnersController = new PartnersController(_repositoryMock.Object);

            // Act.

            var result = await partnersController.SetPartnerPromoCodeLimitAsync(Guid.NewGuid(), requestFixture);

            // Assert.

            result.Should().BeOfType(typeof(BadRequestObjectResult));
        }

        [Fact]
        public async Task SetPartnerPromoCodeLimitAsync_ShouldAddNewLimit_WhenRequestIsCorrect()
        {
            // Arrange.

            var endDate = new DateTime(2030, 1, 1);
            var limit = 10;

            var requestFixture = _fixture.Build<SetPartnerPromoCodeLimitRequest>()
                                        .With(_ => _.Limit, limit)
                                        .With(_ => _.EndDate, endDate)
                                        .Create();

            var partnerFixture = _fixture.Build<Partner>()
                                        .With(_ => _.IsActive, true)
                                        .Create();

            var partnerLimitsCount = partnerFixture.PartnerLimits?.Count() ?? 0;

            _repositoryMock.Setup(_ => _.GetByIdAsync(It.IsAny<Guid>())).Returns(async () => partnerFixture);
            _repositoryMock.Setup(_ => _.UpdateAsync(It.IsAny<Partner>()));

            var partnersController = new PartnersController(_repositoryMock.Object);

            // Act.

            var result = await partnersController.SetPartnerPromoCodeLimitAsync(Guid.NewGuid(), requestFixture);

            // Assert.

            result.Should().BeOfType(typeof(CreatedAtActionResult));
            partnerFixture.PartnerLimits.Should().HaveCount(partnerLimitsCount + 1);

            var lastpartnerFixture = partnerFixture.PartnerLimits.Last();

            lastpartnerFixture.Limit.Should().Be(limit);
            lastpartnerFixture.EndDate.Should().Be(endDate);
            lastpartnerFixture.PartnerId.Should().Be(partnerFixture.Id);

            _repositoryMock.Verify(_ => _.UpdateAsync(It.IsAny<Partner>()), Times.Once);
        }
    }
}