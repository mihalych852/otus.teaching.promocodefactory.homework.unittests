using AutoFixture;
using AutoFixture.AutoMoq;
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
        private readonly Mock<IRepository<Partner>> _partnersRepositoryMock;
        private readonly PartnersController _partnersController;
        private readonly SetPartnerPromoCodeLimitRequest _request;

        public SetPartnerPromoCodeLimitAsyncTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _partnersRepositoryMock = fixture.Freeze<Mock<IRepository<Partner>>>();
            _partnersController = fixture.Build<PartnersController>().OmitAutoProperties().Create();

            var autoFixture = new Fixture();
            _request = autoFixture.Create<SetPartnerPromoCodeLimitRequest>();
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

        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_PartnerIsNotFound_ReturnsNotFound()
        {
            // Arrange
            var partnerId = Guid.NewGuid();
            Partner partner = null;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            //Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, _request);

            //Arrange
            result.Should().BeAssignableTo<NotFoundResult>();
        }

        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_PartnerIsNotActive_ReturnsBadRequest()
        {
            // Arrange
            Partner partner = CreateBasePartner();
            partner.IsActive = false;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            //Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, _request);

            //Arrange
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }

        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_ActiveLimitIsNotNull_ActiveLimitIsZero()
        {
            // Arrange
            Partner partner = CreateBasePartner();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            var activeLimit = partner.PartnerLimits.FirstOrDefault(x => !x.CancelDate.HasValue);

            //Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, _request);

            //Arrange
            Assert.Equal(0, partner.NumberIssuedPromoCodes);
            // result.Should().BeAssignableTo<CreatedAtActionResult>();
        }

        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_SetNewLimit_CancelLimitIsNow()
        {
            // Arrange
            Partner partner = CreateBasePartner();


            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            var activeLimit = partner.PartnerLimits.FirstOrDefault(x => !x.CancelDate.HasValue);

            //Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, _request);

            //Arrange
            Assert.Equal(DateTime.Now.ToShortDateString(), activeLimit.CancelDate.Value.ToShortDateString());
            // result.Should().BeAssignableTo<CreatedAtActionResult>();
        }

        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_SetNewLimit_LimitOverZero()
        {
            // Arrange
            Partner partner = CreateBasePartner();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            var activeLimit = partner.PartnerLimits.FirstOrDefault(x => !x.CancelDate.HasValue);

            //Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, _request);

            //Arrange
            Assert.True(_request.Limit > 0);
            // result.Should().BeAssignableTo<CreatedAtActionResult>();
        }

        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_SaveNewLimit_DbHasNewLimit()
        {
            // Arrange
            Partner partner = CreateBasePartner();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);
            _partnersRepositoryMock.Setup(repo => repo.AddAsync(partner));
            _partnersRepositoryMock.Setup(repo => repo.UpdateAsync(partner));

            //Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, _request);
            var partnerFromDb = await _partnersRepositoryMock.Object.GetByIdAsync(partner.Id);
            var newPartnerLimit = partnerFromDb.PartnerLimits.Where(l => l.Limit == partner.PartnerLimits.FirstOrDefault().Limit).FirstOrDefault();

            //Arrange
            Assert.NotNull(newPartnerLimit);
        }

    }
}