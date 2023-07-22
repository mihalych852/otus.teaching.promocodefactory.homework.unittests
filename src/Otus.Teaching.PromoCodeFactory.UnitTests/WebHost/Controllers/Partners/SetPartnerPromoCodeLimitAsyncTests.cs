using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Namotion.Reflection;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using Otus.Teaching.PromoCodeFactory.DataAccess;
using Otus.Teaching.PromoCodeFactory.DataAccess.Data;
using Otus.Teaching.PromoCodeFactory.DataAccess.Repositories;
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

        public SetPartnerPromoCodeLimitAsyncTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _partnersRepositoryMock = fixture.Freeze<Mock<IRepository<Partner>>>();
            _partnersController = fixture.Build<PartnersController>().OmitAutoProperties().Create();
        }

        //Если партнер не найден, то также нужно выдать ошибку 404;
        [Fact]
        public async void SetPartnerPromoCodeLimitAsyncTests_PatnerIsNotFound_ReturnNotFound()
        {
            Guid guid = Guid.Parse("def47943-7aaf-44a1-ae21-05aa4948b165");

            Partner partner = null;
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(guid)).ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(guid, default(SetPartnerPromoCodeLimitRequest));

            // Assert
            result.Should().BeAssignableTo<NotFoundResult>();
        }

        [Fact]
        //Если партнер заблокирован, то есть поле IsActive=false в классе Partner, то также нужно выдать ошибку 400;
        public async void SetPartnerPromoCodeLimitAsyncTests_PatnerIsNotActive_ReturnNotFound()
        {
            Partner partner = this.CreateBasePartner();
            partner.IsActive = false;
            
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id)).ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, default(SetPartnerPromoCodeLimitRequest));

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }

        [Fact]
        //Если партнеру выставляется лимит, то мы должны обнулить количество промокодов, которые партнер выдал NumberIssuedPromoCodes
        public async void SetPartnerPromoCodeLimitAsyncTests_PartnerSetLimit_NumberIssuedPromoCodesIs0()
        {
            Partner partner = this.CreateBasePartner();
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id)).ReturnsAsync(partner);

            SetPartnerPromoCodeLimitRequest setPartnerPromoCodeLimitRequest =
                new SetPartnerPromoCodeLimitRequest { EndDate = DateTime.Now.AddDays(30), Limit = 1 };

            // Act
            await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, setPartnerPromoCodeLimitRequest);

            // Assert
            Assert.Equal(0, partner.NumberIssuedPromoCodes);
        }

        [Fact]
        //если лимит закончился, то количество не обнуляется
        public async void SetPartnerPromoCodeLimitAsyncTests_ThereIsALimitFinished_NumberIssuedPromoCodesIsNot0()
        {
            Partner partner = this.CreateBasePartner();
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id)).ReturnsAsync(partner);

            SetPartnerPromoCodeLimitRequest setPartnerPromoCodeLimitRequest =
                new SetPartnerPromoCodeLimitRequest { EndDate = DateTime.Now.AddDays(30), Limit = 1 };

            // Act
            partner.PartnerLimits.FirstOrDefault(x => !x.CancelDate.HasValue).CancelDate = DateTime.Now.AddDays(-1);        /*Закончить лимит*/
            await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, setPartnerPromoCodeLimitRequest);           /*Задать лимит пользователю*/

            // Assert
            Assert.NotEqual(0, partner.NumberIssuedPromoCodes);
        }

        [Fact]
        //При установке лимита нужно отключить предыдущий лимит;
        public async void SetPartnerPromoCodeLimitAsyncTests_SetLimit_DisablePreviousLimit()
        {
            Partner partner = this.CreateBasePartner();
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id)).ReturnsAsync(partner);

            SetPartnerPromoCodeLimitRequest setPartnerPromoCodeLimitRequest =
                new SetPartnerPromoCodeLimitRequest { EndDate = DateTime.Now.AddDays(30), Limit = 1 };

            // Act
            PartnerPromoCodeLimit lastActiveLimit = partner.PartnerLimits.LastOrDefault(x => !x.CancelDate.HasValue);       /*Получение последнего неотключенного лимита*/
            await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, setPartnerPromoCodeLimitRequest);           /*Задать лимит пользователю*/

            // Assert
            Assert.True(lastActiveLimit.CancelDate.HasValue);
        }

        [Fact]
        //Лимит должен быть больше 0;
        public async void SetPartnerPromoCodeLimitAsyncTests_SetLimit0_ReturnBadResult()
        {
            Partner partner = this.CreateBasePartner();
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id)).ReturnsAsync(partner);

            SetPartnerPromoCodeLimitRequest setPartnerPromoCodeLimitRequest =
                new SetPartnerPromoCodeLimitRequest { EndDate = DateTime.Now.AddDays(30), Limit = 0 };

            // Act
            IActionResult result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, setPartnerPromoCodeLimitRequest);           /*Задать лимит пользователю*/

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }

        [Fact]
        //Нужно убедиться, что сохранили новый лимит в базу данных(это нужно проверить Unit-тестом);
        public async void SetPartnerPromoCodeLimitAsyncTests_SendLimit_LimitExists()
        {
            Partner partner = this.CreateBasePartner();
            partner.PartnerLimits = new List<PartnerPromoCodeLimit>();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id)).ReturnsAsync(partner);

            SetPartnerPromoCodeLimitRequest setPartnerPromoCodeLimitRequest =
                new SetPartnerPromoCodeLimitRequest { EndDate = DateTime.Now.AddDays(30), Limit = 1 };

            // Act
            IActionResult result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, setPartnerPromoCodeLimitRequest);           /*Задать лимит пользователю*/

            // Assert
            Partner partnerInDB = ((IRepository<Partner>)_partnersRepositoryMock.Object).GetByIdAsync(partner.Id).Result;
            Assert.NotEmpty(partnerInDB.PartnerLimits);
        }


        public Partner CreateBasePartner()
        {
            var partner = new Partner()
            {
                Id = Guid.Parse("7d994823-8226-4273-b063-1a95f3cc1df8"),
                Name = "Суперигрушки",
                IsActive = true,
                NumberIssuedPromoCodes = 1,
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
    }
}