using AutoMapper;
using FluentAssertions;
using Moq;
using ProjectK.API.MappingProfiles;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MemberHandlers
{
    public class GetMemberByKeyHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IMemberRepository> _memberRepoMock;
        private readonly Mock<IMentorAssignmentRepository> _mentorRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ICurrentUserContext> _currentUserContextMock;
        private readonly GetMemberByKeyHandler _handler;

        public GetMemberByKeyHandlerTests()
        {
            _memberRepoMock = new Mock<IMemberRepository>();
            _mentorRepoMock = new Mock<IMentorAssignmentRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.Setup(u => u.Members).Returns(_memberRepoMock.Object);
            _uowMock.Setup(u => u.MentorAssignments).Returns(_mentorRepoMock.Object);

            _mapperMock = new Mock<IMapper>(MockBehavior.Strict);
            
            _currentUserContextMock = new Mock<ICurrentUserContext>();
            _currentUserContextMock.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(true);

            _handler = new GetMemberByKeyHandler(_uowMock.Object, _mapperMock.Object, _currentUserContextMock.Object);
        }

        [Fact]
        public async Task Handle_WhenMemberExists_ShouldReturnSuccessWithMappedData()
        {
            // Arrange
            var memberKey = Guid.NewGuid();
            var groupKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var member = new Member
            {
                MemberKey = memberKey,
                GroupKey = groupKey,
                KurinKey = kurinKey,
                FirstName = "Ivan",
                MiddleName = "I.",
                LastName = "Petrenko",
                Email = "ivan@example.com",
                PhoneNumber = "123456",
                DateOfBirth = new DateOnly(2001, 2, 3)
            };

            _memberRepoMock
                .Setup(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);

            // Mock mapper behavior explicitly to avoid invoking real mapping (and its resolver dependencies)
            _mapperMock
                .Setup(m => m.Map<MemberResponse>(member))
                .Returns(new MemberResponse
                {
                    MemberKey = member.MemberKey,
                    GroupKey = (Guid)member.GroupKey,
                    KurinKey = member.KurinKey,
                    FirstName = member.FirstName,
                    MiddleName = member.MiddleName,
                    LastName = member.LastName,
                    Email = member.Email,
                    PhoneNumber = member.PhoneNumber,
                    DateOfBirth = member.DateOfBirth,
                    ProfilePhotoUrl = null
                });

            var query = new GetMemberByKey(memberKey);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.Success);
            result.Data.Should().NotBeNull();
            result.Data!.MemberKey.Should().Be(memberKey);
            result.Data.GroupKey.Should().Be(groupKey);
            result.Data.KurinKey.Should().Be(kurinKey);
            result.Data.FirstName.Should().Be("Ivan");
            result.Data.LastName.Should().Be("Petrenko");

            _memberRepoMock.Verify(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()), Times.Once);
            _mapperMock.Verify(m => m.Map<MemberResponse>(member), Times.Once);
            _mapperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_WhenMemberDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var memberKey = Guid.NewGuid();
            _memberRepoMock
                .Setup(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Member)null!);

            var query = new GetMemberByKey(memberKey);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.NotFound);
            result.Data.Should().BeNull();

            _memberRepoMock.Verify(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()), Times.Once);
            // Mapper should never be called when entity not found
            _mapperMock.VerifyNoOtherCalls();
        }
    }
}
