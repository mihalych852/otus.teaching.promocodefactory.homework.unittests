using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using Otus.Teaching.PromoCodeFactory.WebHost.Models;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.Helpers;

public class TestDataGenerator
{
    private readonly IFixture _fixture;
    
    public TestDataGenerator(IFixture fixture)
    {
        _fixture = fixture;
    }
    
    public Guid GetRandomUuid() 
        => Guid.NewGuid();

    public SetPartnerPromoCodeLimitRequest GetSetPartnerPromoCodeLimitRequest() =>
        _fixture.Build<SetPartnerPromoCodeLimitRequest>().Create();

    public Partner GetPartner(bool? isActive, List<PartnerPromoCodeLimit> promoCodes = null) =>
        _fixture
            .Build<Partner>()
            .With(x => x.IsActive, isActive)
            .With(x => x.PartnerLimits, promoCodes)
            .Create();

    public PartnerPromoCodeLimit GetPartnerPromoCodeLimit(DateTime? limit = null) =>
        _fixture
            .Build<PartnerPromoCodeLimit>()
            .With(x => x.CancelDate, limit)
            .Create();
}