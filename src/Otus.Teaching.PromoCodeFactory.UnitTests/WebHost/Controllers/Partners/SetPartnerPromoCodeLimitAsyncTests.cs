using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using Otus.Teaching.PromoCodeFactory.DataAccess;
using Otus.Teaching.PromoCodeFactory.DataAccess.Repositories;
using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
using Otus.Teaching.PromoCodeFactory.WebHost.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    public class SetPartnerPromoCodeLimitAsyncTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IRepository<Partner>> _partnersRepositoryMock;
        private readonly PartnersController _partnersController;

        public SetPartnerPromoCodeLimitAsyncTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            _partnersRepositoryMock = _fixture.Freeze<Mock<IRepository<Partner>>>();
            _partnersController = _fixture.Build<PartnersController>().OmitAutoProperties().Create();
        }

        /// <summary>
        /// Если партнер не найден, то также нужно выдать ошибку 404;
        /// </summary>
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_PartnerIsNotFound_ReturnsNotFound()
        {
            // Arrange
            var partnerId = Guid.Parse("def47943-7aaf-44a1-ae21-05aa4948b165");

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync((Partner)null);

            var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, request);

            // Assert
            result.Should().BeAssignableTo<NotFoundResult>();
        }

        /// <summary>
        /// Если партнер заблокирован, то есть поле IsActive=false в классе Partner, то также нужно выдать ошибку 400;
        /// </summary>
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_PartnerIsNotActive_ReturnsBadRequest()
        {
            // Arrange
            var partner = _fixture.Build<Partner>()
                .With(p => p.IsActive, false)
                .Create();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            var request = _fixture.Create<SetPartnerPromoCodeLimitRequest>();

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }

        /// <summary>
        /// Если партнеру выставляется лимит, то мы должны обнулить количество промокодов,
        /// которые партнер выдал NumberIssuedPromoCodes
        /// </summary>
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_NewPartnerLimit_ResetNumberIssuedPromoCodes()
        {
            // Arrange
            var partnerLimits = _fixture.Build<PartnerPromoCodeLimit>()
                .With(p => p.EndDate, DateTime.Now.AddDays(1))
                .With(p => p.CancelDate, (DateTime?)null)
                .Create();

            var partner = _fixture.Build<Partner>()
                .With(p => p.IsActive, true)
                .With(p => p.NumberIssuedPromoCodes, 100)
                .With(p => p.PartnerLimits, new List<PartnerPromoCodeLimit> { partnerLimits })
                .Create();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            var request = _fixture.Build<SetPartnerPromoCodeLimitRequest>()
                .With(r => r.Limit, 10)
                .Create();

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            result.Should().BeAssignableTo<CreatedAtActionResult>();
            partner.NumberIssuedPromoCodes.Should().Be(0);
        }

        /// <summary>
        /// Если партнеру выставляется новый лимит, а существующий лимит закончился,
        /// то количество NumberIssuedPromoCodes не обнуляется;
        /// </summary>
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_NewPartnerLimitAndLimitExpire_NotResetNumberIssuedPromoCodes()
        {
            // Arrange
            var expirePartnerLimits = _fixture.Build<PartnerPromoCodeLimit>()
                .With(p => p.EndDate, DateTime.Now.AddDays(-1))
                .With(p => p.CancelDate, (DateTime?)null)
                .Create();

            var partner = _fixture.Build<Partner>()
                .With(p => p.IsActive, true)
                .With(p => p.NumberIssuedPromoCodes, 100)
                .With(p => p.PartnerLimits, new List<PartnerPromoCodeLimit> { expirePartnerLimits })
                .Create();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            var request = _fixture.Build<SetPartnerPromoCodeLimitRequest>()
                .With(r => r.Limit, 10)
                .Create();

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            result.Should().BeAssignableTo<CreatedAtActionResult>();
            partner.NumberIssuedPromoCodes.Should().Be(100);
        }

        /// <summary>
        /// При установке лимита нужно отключить предыдущий лимит
        /// </summary>
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_NewPartnerLimit_CancelLimit()
        {
            // Arrange
            var partnerLimits = _fixture.Build<PartnerPromoCodeLimit>()
                .With(p => p.EndDate, DateTime.Now.AddDays(1))
                .With(p => p.CancelDate, (DateTime?)null)
                .Create();

            var partner = _fixture.Build<Partner>()
                .With(p => p.IsActive, true)
                .With(p => p.PartnerLimits, new List<PartnerPromoCodeLimit> { partnerLimits })
                .Create();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            var request = _fixture.Build<SetPartnerPromoCodeLimitRequest>()
                .With(r => r.Limit, 10)
                .Create();

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            result.Should().BeAssignableTo<CreatedAtActionResult>();
            partnerLimits.CancelDate.Should().NotBeNull();
        }

        /// <summary>
        /// Лимит должен быть больше 0;
        /// </summary>
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_NewPartnerLimitLessZero_ReturnBadRequest()
        {
            // Arrange
            var partner = _fixture.Build<Partner>()
                .With(p => p.IsActive, true)
                .Create();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            var request = _fixture.Build<SetPartnerPromoCodeLimitRequest>()
                .With(r => r.Limit, -1)
                .Create();

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }

        /// <summary>
        /// Нужно убедиться, что сохранили новый лимит в базу данных (это нужно проверить Unit-тестом);
        /// </summary>
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_NewPartnerLimit_AddToDb()
        {
            // Arrange
            var serviceProvider = BuildServiceCollection();

            var partner = _fixture.Build<Partner>()
                .With(p => p.IsActive, true)
                .With(p => p.PartnerLimits, new List<PartnerPromoCodeLimit>())
                .Create();

            var partnerRepository = serviceProvider.GetRequiredService<IRepository<Partner>>();
            await partnerRepository.AddAsync(partner);

            var request = _fixture.Build<SetPartnerPromoCodeLimitRequest>()
                .With(r => r.Limit, 10)
                .Create();

            var parentController = new PartnersController(partnerRepository);

            // Act
            var result = await parentController.SetPartnerPromoCodeLimitAsync(partner.Id, request);
            var resultPartner = await partnerRepository.GetByIdAsync(partner.Id);

            // Assert
            result.Should().BeAssignableTo<CreatedAtActionResult>();
            resultPartner.PartnerLimits.First().PartnerId.Should().Be(partner.Id);
            resultPartner.PartnerLimits.First().Limit.Should().Be(request.Limit);
            resultPartner.PartnerLimits.First().EndDate.Should().Be(request.EndDate);
        }

        private IServiceProvider BuildServiceCollection()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddDbContext<DataContext>(cfg => cfg.UseInMemoryDatabase("PromoCodeFactoryDb"));
            serviceCollection.AddTransient(typeof(IRepository<>), typeof(EfRepository<>));
            
            return serviceCollection.BuildServiceProvider();
        }

    }
}