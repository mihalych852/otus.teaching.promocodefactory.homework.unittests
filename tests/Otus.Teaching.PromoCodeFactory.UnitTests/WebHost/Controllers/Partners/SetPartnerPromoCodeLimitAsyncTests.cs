using System.ComponentModel.Design;
using System.Reflection;
using System;
using NUnit.Framework;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using Moq;
using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
using System.Threading.Tasks;
using Otus.Teaching.PromoCodeFactory.WebHost.Models;
using Microsoft.AspNetCore.Mvc;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Otus.Teaching.PromoCodeFactory.DataAccess.Repositories;
using Otus.Teaching.PromoCodeFactory.DataAccess;
using FluentAssertions;
using System.Runtime.CompilerServices;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    [TestFixture]
    public class SetPartnerPromoCodeLimitAsyncTests
    {
        private readonly PartnersController _controller;
        private readonly IFixture _fixture;
        private readonly Mock<IRepository<Partner>> _repositoryMock;
        private readonly Guid _existPartner;

        public SetPartnerPromoCodeLimitAsyncTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));
            _existPartner = _fixture.Create<Guid>();

            _repositoryMock = _fixture.Freeze<Mock<IRepository<Partner>>>();
           
            _controller = _fixture.Build<PartnersController>().OmitAutoProperties().Create();
        }

        [Test]
        public void Partner_NotExist_Error404()
        {
            // Given
            var obj = _fixture.Build<Partner>().WithAutoProperties().With(p => p.IsActive, true).Create();
            _repositoryMock.Setup(m => m.GetByIdAsync(It.Is<Guid>(g => g == _existPartner))).ReturnsAsync(obj);
            _repositoryMock.Setup(m => m.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Partner)null);
            // When
            var result = _controller.SetPartnerPromoCodeLimitAsync(Guid.NewGuid(), new SetPartnerPromoCodeLimitRequest()).GetAwaiter().GetResult();
            // Then
            result.Should().BeOfType<NotFoundResult>();
        }

        [Test]
        public void Partner_Blocked_Error400()
        {
            // Given
            var obj = _fixture.Build<Partner>().WithAutoProperties().With(p => p.IsActive, false).Create();
            _repositoryMock.Setup(m => m.GetByIdAsync(It.Is<Guid>(g => g == _existPartner))).ReturnsAsync(obj);
            // When
            var result = _controller.SetPartnerPromoCodeLimitAsync(_existPartner, new SetPartnerPromoCodeLimitRequest()).GetAwaiter().GetResult();
            // Then
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public void PartnerSetLimit_Limit_MustResetTo0()
        {
            // Given 
            var obj = _fixture.Build<Partner>()
            .Without(p => p.PartnerLimits)
            .With(p => p.NumberIssuedPromoCodes, 100)
            .With(p => p.Id, _existPartner)
            .With(p => p.IsActive, true)
            .Do(p => p.PartnerLimits = new List<PartnerPromoCodeLimit>())
            .Do(p => p.PartnerLimits.AddMany(()=>new PartnerPromoCodeLimit(){Limit = 1}, 3))
            .Create();

            var request = _fixture.Build<SetPartnerPromoCodeLimitRequest>()
            .With(p => p.Limit, 1)
            .Create();

            _repositoryMock.Setup(m => m.GetByIdAsync(It.Is<Guid>(g => g == _existPartner))).ReturnsAsync(obj);
            // When
            var result = _controller.SetPartnerPromoCodeLimitAsync(_existPartner, request).GetAwaiter().GetResult();;
            // Then
            result.Should().BeOfType<CreatedAtActionResult>();
            obj.NumberIssuedPromoCodes.Should().Be(0); 
        }

        [Test]
        public void PartnerSetLimit_Limit_MustResetNOTTo0()
        {
            // Given
            var limitsCount = 3; 
            var obj = _fixture.Build<Partner>()
            .Without(p => p.PartnerLimits)
            .With(p => p.NumberIssuedPromoCodes, 100)
            .With(p => p.Id, _existPartner)
            .With(p => p.IsActive, true)
            .Do(p => p.PartnerLimits = new List<PartnerPromoCodeLimit>())
            .Do(p => p.PartnerLimits.AddMany(()=>new PartnerPromoCodeLimit(){Limit = 1}, limitsCount))
            .Create();

            var request = _fixture.Build<SetPartnerPromoCodeLimitRequest>()
            .With(p => p.Limit, 1)
            .Create();

            _repositoryMock.Setup(m => m.GetByIdAsync(It.Is<Guid>(g => g == _existPartner))).ReturnsAsync(obj);
            // When
            var result = _controller.SetPartnerPromoCodeLimitAsync(_existPartner, request).GetAwaiter().GetResult();
            // Then

            result.Should().BeOfType<CreatedAtActionResult>();
            obj.PartnerLimits.Count.Should().Be(limitsCount + 1);
        }

        [Test]
        public void PartnerSetLimit_PrevLimit_MustDisabled()
        {
            // Given
            var limitsCount = 3; 
            var obj = _fixture.Build<Partner>()
            .Without(p => p.PartnerLimits)
            .With(p => p.NumberIssuedPromoCodes, 100)
            .With(p => p.Id, _existPartner)
            .With(p => p.IsActive, true)
            .Do(p => p.PartnerLimits = new List<PartnerPromoCodeLimit>())
            .Do(p => p.PartnerLimits.AddMany(()=>new PartnerPromoCodeLimit(){Limit = 1}, limitsCount))
            .Create();

            var request = _fixture.Build<SetPartnerPromoCodeLimitRequest>()
            .With(p => p.Limit, 1)
            .Create();

            _repositoryMock.Setup(m => m.GetByIdAsync(It.Is<Guid>(g => g == _existPartner))).ReturnsAsync(obj);
            // When
            var result = _controller.SetPartnerPromoCodeLimitAsync(_existPartner, request).GetAwaiter().GetResult();;
            // Then
            result.Should().BeOfType<CreatedAtActionResult>();
            obj.PartnerLimits.Should().ContainSingle(p => p.CancelDate.HasValue);
        }

        [Test]
        public void PartnerSetNewLimit_Limit_MustBeMore0()
        {
            // Given 
            var obj = _fixture.Build<Partner>()
            .With(p => p.Id, _existPartner)
            .Create();

            var request = _fixture.Build<SetPartnerPromoCodeLimitRequest>()
            .With(p => p.Limit, 0)
            .Create();

            _repositoryMock.Setup(m => m.GetByIdAsync(It.Is<Guid>(g => g == _existPartner))).ReturnsAsync(obj);
            // When
            var result = _controller.SetPartnerPromoCodeLimitAsync(_existPartner, request).GetAwaiter().GetResult();;
            // Then
            result.Should().BeOfType<BadRequestObjectResult>(); 
        }

        [Test]
        public void PartnerSetLimit_Database_UpdatedValue()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddDbContext<DataContext>(x =>
            {
                x.UseInMemoryDatabase("database");
                x.UseSnakeCaseNamingConvention();
                x.UseLazyLoadingProxies();
            });
            services.AddScoped<PartnersController>();

            using(var sp = services.BuildServiceProvider())
            {
                var controller = sp.GetService<PartnersController>();
                var repository = sp.GetService<IRepository<Partner>>();
                // Given
                Guid existPartner = _fixture.Create<Guid>();
                var obj = _fixture.Build<Partner>()
                .Without(p => p.PartnerLimits)
                .With(p => p.NumberIssuedPromoCodes, 100)
                .With(p => p.Id, existPartner)
                .With(p => p.IsActive, true)
                .Do(p => p.PartnerLimits = new List<PartnerPromoCodeLimit>())
                .Do(p => p.PartnerLimits.AddMany(()=>new PartnerPromoCodeLimit(){Limit = 1}, 3))
                .Create();
                // When
                repository.AddAsync(obj).GetAwaiter().GetResult();;

                var request = _fixture.Build<SetPartnerPromoCodeLimitRequest>()
                .Create();

                var result = controller.SetPartnerPromoCodeLimitAsync(existPartner, request).GetAwaiter().GetResult();;

                var updated = repository.GetByIdAsync(existPartner).GetAwaiter().GetResult();;
                // Then
                updated.PartnerLimits.Should().ContainSingle(p => p.Limit == request.Limit);
            }
 
        }
    }
}