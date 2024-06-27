namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    using AutoFixture.AutoMoq;
    using AutoFixture;
    using Xunit;
    using Moq;
    using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
    using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
    using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
    using Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Builders;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    using System.Linq;

    public class SetPartnerPromoCodeLimitAsyncTests
    {
        private readonly Mock<IRepository<Partner>> _partnersRepositoryMock;
        private readonly PartnersController _partnersController;
        private readonly SetPartnerPromoCodeLimitAsyncTestBuilder _builder; 

        public SetPartnerPromoCodeLimitAsyncTests()
        {
            _builder = new SetPartnerPromoCodeLimitAsyncTestBuilder();
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _partnersRepositoryMock = fixture.Freeze<Mock<IRepository<Partner>>>();
            _partnersController = fixture.Build<PartnersController>().OmitAutoProperties().Create();
        }

        /// <summary>
        ///     Лимит оказался <= 0, код 400
        /// </summary>
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_LimitLessThanZero_BadRequest()
        {
            // Arrange
            var id = _builder.GetGuid();
            var request = _builder.GetRequestWithWrongLimit();

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(id, request);

            // Assert
            Assert.True(result is BadRequestObjectResult);
        }

        /// <summary>
        ///     Не найден партнер, код 404
        /// </summary>
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_PartnerIsNotFound_NotFound()
        {
            // Arrange
            var id = _builder.GetGuid();
            var request = _builder.GetRequest();
            
            _partnersRepositoryMock
                .Setup(x => x.GetByIdAsync(id))
                .Returns(Task.FromResult<Partner>(null));

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(id, request);

            // Assert
            Assert.True(result is NotFoundResult);
        }

        /// <summary>
        ///     Партнер уже не активен, код 400
        /// </summary>
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_PartnerIsNotActive_BadRequest()
        {
            // Arrange
            var id = _builder.GetGuid();
            var request = _builder.GetRequest();
            var partner = _builder.GetPartner();
            partner.IsActive = false;

            _partnersRepositoryMock
                .Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(id, request);

            // Assert
            Assert.True(result is BadRequestObjectResult);
        }

        /// <summary>
        ///     У партнера есть активный лимит, количество истекших промокодов обнулилось и предыдущий лимит не активен
        /// </summary>
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_LimitExists_NumberIssuedPromoCodesIsZeroAndPreviousLimitDisabled()
        {
            // Arrange
            var id = _builder.GetGuid();
            var request = _builder.GetRequest();
            var partner = _builder.GetPartnerWithLimit(false);

            var previousLimit = partner.PartnerLimits.ToList().First();

            _partnersRepositoryMock
               .Setup(x => x.GetByIdAsync(id))
               .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(id, request);

            // Assert
            Assert.Equal(0, partner.NumberIssuedPromoCodes);
            Assert.True(previousLimit.CancelDate is not null);
        }

        /// <summary>
        ///     У партнера нет активных лимитов, количество истекших промокодов не изменилось
        /// </summary>
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_LimitDoesNotExists_NumberIssuedPromoCodesHasNotChanged()
        {
            // Arrange
            var id = _builder.GetGuid();
            var request = _builder.GetRequest();
            var partner = _builder.GetPartnerWithLimit(true);

            var expected = partner.NumberIssuedPromoCodes;

            _partnersRepositoryMock
               .Setup(x => x.GetByIdAsync(id))
               .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(id, request);

            // Assert
            Assert.Equal(expected, partner.NumberIssuedPromoCodes);
        }

        /// <summary>
        ///     Новый лимит создался и был записан в бд
        /// </summary>
        [Fact]
        public async void SetPartnerPromoCodeLimitAsync_NewLimitCreated_NewLimitInDb()
        {
            // Arrange
            var context = _builder.GetContext();
            var request = _builder.GetRequest();
            var partner = _builder.GetPartnerWithLimit(true);
            _builder.AddPartnerToDb(partner, context);
            var repositoryWithInMemoryDb = _builder.GetRepositoryWithInMemoryDb(context);
            var controllerWithInMemoryDb = _builder.GetPartnersControllerWithInMemoryDb(repositoryWithInMemoryDb);

            // Act
            var result = await controllerWithInMemoryDb.SetPartnerPromoCodeLimitAsync(partner.Id, request);

            // Assert
            var partnerFromDb = await repositoryWithInMemoryDb.GetByIdAsync(partner.Id);
            var newLimit = partnerFromDb.PartnerLimits.FirstOrDefault(x => x.Limit == request.Limit
                                                                            && x.EndDate == request.EndDate);
            Assert.NotNull(newLimit);
        }
    }
}