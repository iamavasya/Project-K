using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Project;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.BusinessLogic.Services.Audit;
using ProjectK.BusinessLogic.Tests.TestHelpers;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Audit;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Tests.ProbesAndBadgesModule.HandlerTests;

/// <summary>
/// The third probe's projects: limits held by the server, pictures only from the project itself,
/// and files that go when their row goes.
/// </summary>
public class ProbeProjectHandlerTests
{
    private const string ProbeId = "probe-3";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProbeProjectRepository> _projects = new();
    private readonly Mock<IProbeProjectAccess> _access = new();
    private readonly Mock<IBlobReadLinks> _links = new();
    private readonly Mock<IPhotoService> _photos = new();
    private readonly Mock<IProbesCatalogService> _probes = new();
    private readonly Mock<ICurrentUserContext> _user = new();
    private readonly JournalRecorder _journal;
    private readonly Guid _memberKey = Guid.NewGuid();

    public ProbeProjectHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.ProbeProjects).Returns(_projects.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _access.Setup(a => a.IsProjectProbeAsync(_memberKey, ProbeId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _access.Setup(a => a.CanEditAsync(_memberKey, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _links.Setup(l => l.For(It.IsAny<string?>())).Returns<string?>(name => $"https://blob/{name}?sig");
        _user.SetupGet(u => u.UserId).Returns(Guid.NewGuid());
        _user.SetupGet(u => u.KurinKey).Returns(Guid.NewGuid());
        _journal = new JournalRecorder(_user.Object, new AuditActorResolver(_user.Object, new Mock<IMemberDirectory>().Object));
        _projects.Setup(r => r.GetForMemberProbeAsync(_memberKey, ProbeId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private SaveProbeProjectCommandHandler SaveHandler() => new(
        _unitOfWork.Object, _access.Object, _links.Object, _photos.Object, _journal.Actors, _journal.Audit, _probes.Object);

    private UploadProbeProjectImageCommandHandler UploadHandler() => new(
        _unitOfWork.Object, _access.Object, _photos.Object, _links.Object);

    private ProbeProject GivenProject(int images = 0)
    {
        var project = new ProbeProject { MemberKey = _memberKey, ProbeId = ProbeId, Title = "Похід" };
        for (var i = 0; i < images; i++)
        {
            project.Images.Add(new ProbeProjectImage { ProbeProjectKey = project.ProbeProjectKey, BlobName = $"probe-project-images/{i}.jpg" });
        }

        _projects.Setup(r => r.GetWithImagesAsync(project.ProbeProjectKey, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        return project;
    }

    private SaveProbeProjectCommand Save(Guid? projectKey, string description, string title = "Похід") =>
        new(_memberKey, ProbeId, projectKey, title, true, description, "Олена Коваль", "+380501112233");

    [Fact]
    public async Task Save_ShouldAddAProject_AndJournalIt()
    {
        var result = await SaveHandler().Handle(Save(null, "**План** походу"), CancellationToken.None);

        result.Type.Should().Be(ResultType.Created);
        result.Data!.IsIndividual.Should().BeTrue();
        result.Data.DirectionMentorName.Should().Be("Олена Коваль");
        _projects.Verify(r => r.Create(It.IsAny<ProbeProject>(), It.IsAny<CancellationToken>()), Times.Once);
        _journal.Entries.Should().ContainSingle(e => e.Action == AuditActions.Probe.ProjectAdded);
    }

    [Fact]
    public async Task Save_ShouldRefuseTheSixthProject()
    {
        var five = Enumerable.Range(0, ProbeProjectRules.MaxProjects).Select(_ => new ProbeProject()).ToList();
        _projects.Setup(r => r.GetForMemberProbeAsync(_memberKey, ProbeId, It.IsAny<CancellationToken>())).ReturnsAsync(five);

        var result = await SaveHandler().Handle(Save(null, "text"), CancellationToken.None);

        result.Type.Should().Be(ResultType.BadRequest);
        result.ErrorCode.Should().Be("TooManyProjects");
    }

    [Fact]
    public async Task Save_ShouldRefuseADescriptionOverTheLimit()
    {
        var result = await SaveHandler().Handle(
            Save(null, new string('а', ProbeProjectRules.MaxDescriptionLength + 1)), CancellationToken.None);

        result.ErrorCode.Should().Be("DescriptionTooLong");
    }

    [Theory]
    [InlineData("![x](https://example.com/cat.png)")]
    [InlineData("![x](data:image/png;base64,AAAA)")]
    public async Task Save_ShouldRefuseAPictureFromOutside(string markdown)
    {
        var project = GivenProject();

        var result = await SaveHandler().Handle(Save(project.ProbeProjectKey, markdown), CancellationToken.None);

        result.ErrorCode.Should().Be("ForeignImage");
    }

    [Fact]
    public async Task Save_ShouldRefuseAPictureOfAnotherProject()
    {
        var project = GivenProject();

        var result = await SaveHandler().Handle(
            Save(project.ProbeProjectKey, $"![фото](project-image:{Guid.NewGuid()})"), CancellationToken.None);

        result.ErrorCode.Should().Be("ForeignImage");
    }

    [Fact]
    public async Task Save_ShouldDropPicturesTheTextNoLongerShows_RowsAndBlobs()
    {
        var project = GivenProject(images: 2);
        var kept = project.Images.First();
        var dropped = project.Images.Last();

        var result = await SaveHandler().Handle(
            Save(project.ProbeProjectKey, $"Ось:\n\n![фото](project-image:{kept.ProbeProjectImageKey})"), CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        result.Data!.Images.Should().ContainSingle(image => image.ImageKey == kept.ProbeProjectImageKey);
        _projects.Verify(r => r.RemoveImages(It.Is<IEnumerable<ProbeProjectImage>>(list => list.Single() == dropped)), Times.Once);
        _photos.Verify(p => p.DeletePhotoAsync(dropped.BlobName, It.IsAny<CancellationToken>()), Times.Once);
        _photos.Verify(p => p.DeletePhotoAsync(kept.BlobName, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Save_ShouldBeForbidden_ToSomeoneWhoMayOnlyRead()
    {
        _access.Setup(a => a.CanEditAsync(_memberKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await SaveHandler().Handle(Save(null, "text"), CancellationToken.None);

        result.Type.Should().Be(ResultType.Forbidden);
    }

    [Fact]
    public async Task Save_ShouldRefuse_AProbeThatIsNotTheThird()
    {
        _access.Setup(a => a.IsProjectProbeAsync(_memberKey, ProbeId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await SaveHandler().Handle(Save(null, "text"), CancellationToken.None);

        result.ErrorCode.Should().Be("NotProjectProbe");
    }

    [Fact]
    public async Task Upload_ShouldRefuseAFileOverOneMegabyte_BeforeStoringIt()
    {
        var project = GivenProject();

        var result = await UploadHandler().Handle(
            new UploadProbeProjectImageCommand(_memberKey, ProbeId, project.ProbeProjectKey, new byte[ProbeProjectRules.MaxImageBytes + 1], "big.png"),
            CancellationToken.None);

        result.ErrorCode.Should().Be("ImageTooLarge");
        _photos.Verify(p => p.UploadPhotoAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<BlobUploadContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Upload_ShouldRefuseTheSixthPicture()
    {
        var project = GivenProject(images: ProbeProjectRules.MaxImagesPerProject);

        var result = await UploadHandler().Handle(
            new UploadProbeProjectImageCommand(_memberKey, ProbeId, project.ProbeProjectKey, [1, 2, 3], "a.png"),
            CancellationToken.None);

        result.ErrorCode.Should().Be("TooManyImages");
    }

    [Fact]
    public async Task Upload_ShouldRefuseAFileThatDoesNotDecode()
    {
        var project = GivenProject();
        _photos.Setup(p => p.UploadPhotoAsync(It.IsAny<byte[]>(), It.IsAny<string>(), BlobUploadContext.ProbeProjectImage, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not an image"));

        var result = await UploadHandler().Handle(
            new UploadProbeProjectImageCommand(_memberKey, ProbeId, project.ProbeProjectKey, [1, 2, 3], "evil.png"),
            CancellationToken.None);

        result.ErrorCode.Should().Be("InvalidImageContent");
        _projects.Verify(r => r.AddImage(It.IsAny<ProbeProjectImage>()), Times.Never);
    }

    [Fact]
    public async Task Upload_ShouldStoreThePicture_InTheProjectFolder()
    {
        var project = GivenProject();
        _photos.Setup(p => p.UploadPhotoAsync(It.IsAny<byte[]>(), "a.png", BlobUploadContext.ProbeProjectImage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PhotoUploadResult("probe-project-images/new.jpg", "ignored"));

        var result = await UploadHandler().Handle(
            new UploadProbeProjectImageCommand(_memberKey, ProbeId, project.ProbeProjectKey, [1, 2, 3], "a.png"),
            CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        result.Data!.Url.Should().Be("https://blob/probe-project-images/new.jpg?sig");
        _projects.Verify(r => r.AddImage(It.Is<ProbeProjectImage>(i => i.BlobName == "probe-project-images/new.jpg")), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheProject_AndEveryBlobOfIt()
    {
        var project = GivenProject(images: 2);
        var handler = new DeleteProbeProjectCommandHandler(_unitOfWork.Object, _access.Object, _photos.Object, _journal.Audit, _probes.Object);

        var result = await handler.Handle(new DeleteProbeProjectCommand(_memberKey, ProbeId, project.ProbeProjectKey), CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        _projects.Verify(r => r.Delete(project, It.IsAny<CancellationToken>()), Times.Once);
        _photos.Verify(p => p.DeletePhotoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _journal.Entries.Should().ContainSingle(e => e.Action == AuditActions.Probe.ProjectRemoved);
    }

    [Fact]
    public void Markdown_ShouldReadThePictureKeys_InOrder()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        ProbeProjectMarkdown.ImageKeys($"a ![x](project-image:{first}) b ![](project-image:{second}) [link](https://plast.org.ua)")
            .Should().Equal(first, second);
    }
}
