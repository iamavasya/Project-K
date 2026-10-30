using Moq;
using ProjectK.BusinessLogic.Modules.DuesModule.Services;
using ProjectK.BusinessLogic.Services.Events;
using ProjectK.Common.Interfaces.Modules.DuesModule;
using ProjectK.Common.Interfaces.Modules.KraiModule;
using ProjectK.Common.Models.Dues;
using ProjectK.Common.Models.Events;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.DuesModule;

/// <summary>
/// Released at the start of a quarter, a person does not take it: the charge made for it the day it
/// began goes, and every quarter before it stays.
/// </summary>
public class DuesLeavingQuarterHandlerTests
{
    [Fact]
    public async Task Leaving_DropsTheChargesFromTheQuarterTheyLeftIn()
    {
        var kurinKey = Guid.NewGuid();
        var membershipKey = Guid.NewGuid();
        var charges = new Mock<IDuesChargeRepository>();
        var dues = new Mock<IDuesUnitOfWork>();
        dues.SetupGet(d => d.DuesCharges).Returns(charges.Object);

        // No край profile: Europe/Kyiv. 2 October 2026 is in the fourth quarter.
        await new DuesLeavingQuarterEventHandler(dues.Object, Mock.Of<IKraiProfileProvider>())
            .Handle(new DomainEventNotification<MembershipLeft>(
                new MembershipLeft(membershipKey, kurinKey, new DateTime(2026, 10, 2, 9, 0, 0, DateTimeKind.Utc))), CancellationToken.None);

        charges.Verify(c => c.DeleteFromQuarterAsync(membershipKey, new DuesQuarter(2026, 4).Index, It.IsAny<CancellationToken>()), Times.Once);
    }
}
