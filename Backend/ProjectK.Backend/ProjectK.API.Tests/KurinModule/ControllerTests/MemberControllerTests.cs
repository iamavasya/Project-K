using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProjectK.API.Controllers.KurinModule;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Delete;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Models.Dtos.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProjectK.API.Tests.KurinModule.ControllerTests
{
    public class MemberControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly MemberController _controller;

        public MemberControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new MemberController(_mediatorMock.Object);
        }

        [Fact]
        public async Task GetByKey_ShouldReturnOk_WhenSuccess()
        {
            var key = Guid.NewGuid();
            var dto = new MemberResponse
            {
                MemberKey = key,
                GroupKey = Guid.NewGuid(),
                KurinKey = Guid.NewGuid(),
                FirstName = "Ivan",
                MiddleName = "I.",
                LastName = "Petrenko",
                Email = "ivan@example.com",
                PhoneNumber = "123",
                DateOfBirth = new DateOnly(2000, 1, 1)
            };
            var serviceResult = new ServiceResult<MemberResponse>(ResultType.Success, dto);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetMemberByKey>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.GetByKey(key);

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<MemberResponse>(ok.Value);
            Assert.Equal(key, data.MemberKey);
        }

        [Fact]
        public async Task GetByKey_ShouldReturnNotFound_WhenNotFound()
        {
            var key = Guid.NewGuid();
            var serviceResult = new ServiceResult<MemberResponse>(ResultType.NotFound);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetMemberByKey>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.GetByKey(key);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk_WhenSuccess()
        {
            var members = new List<MemberResponse>
            {
                new() { MemberKey = Guid.NewGuid(), GroupKey = Guid.NewGuid(), KurinKey = Guid.NewGuid(), FirstName = "A", LastName = "L", MiddleName="M", Email="a@ex.com", PhoneNumber="1", DateOfBirth = new DateOnly(1990,1,1) },
                new() { MemberKey = Guid.NewGuid(), GroupKey = Guid.NewGuid(), KurinKey = Guid.NewGuid(), FirstName = "B", LastName = "L", MiddleName="M", Email="b@ex.com", PhoneNumber="2", DateOfBirth = new DateOnly(1991,1,1) }
            };
            var serviceResult = new ServiceResult<IEnumerable<MemberResponse>>(ResultType.Success, members);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetMembers>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<MemberResponse>>(ok.Value);
            Assert.Equal(members.Count, data.Count);
        }

        [Fact]
        public async Task Create_ShouldReturnCreated_WhenSuccess()
        {
            var key = Guid.NewGuid();
            var request = new UpsertMemberRequest
            {
                GroupKey = Guid.NewGuid(),
                FirstName = "Ivan",
                MiddleName = "I.",
                LastName = "Petrenko",
                Email = "ivan@example.com",
                PhoneNumber = "123456",
                DateOfBirth = new DateOnly(2000, 5, 10)
            };

            var serviceResult = new ServiceResult<MemberResponse>(
                ResultType.Created,
                new MemberResponse
                {
                    MemberKey = key,
                    GroupKey = request.GroupKey,
                    KurinKey = Guid.NewGuid(),
                    FirstName = request.FirstName,
                    MiddleName = request.MiddleName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    DateOfBirth = request.DateOfBirth
                },
                "GetByKey",
                new { memberKey = key });

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpsertMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.Create(request, CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var data = Assert.IsType<MemberResponse>(created.Value);
            Assert.Equal(key, data.MemberKey);

            _mediatorMock.Verify(m => m.Send(
                It.Is<UpsertMember>(c =>
                    c.GroupKey == request.GroupKey &&
                    c.FirstName == request.FirstName &&
                    c.LastName == request.LastName),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenInvalid()
        {
            var request = new UpsertMemberRequest
            {
                GroupKey = Guid.NewGuid(),
                FirstName = "Bad",
                MiddleName = "X",
                LastName = "User",
                Email = "bad@example.com",
                PhoneNumber = "000",
                DateOfBirth = new DateOnly(1999, 1, 1)
            };

            var serviceResult = new ServiceResult<MemberResponse>(ResultType.BadRequest, new MemberResponse());

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpsertMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.Create(request, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_ShouldReturnOk_WhenSuccess()
        {
            var memberKey = Guid.NewGuid();
            var request = new UpsertMemberRequest
            {
                GroupKey = Guid.NewGuid(),
                FirstName = "Updated",
                MiddleName = "U.",
                LastName = "Name",
                Email = "upd@example.com",
                PhoneNumber = "555",
                DateOfBirth = new DateOnly(1995, 2, 2),
                RemoveProfilePhoto = false
            };

            var serviceResult = new ServiceResult<MemberResponse>(ResultType.Success,
                new MemberResponse
                {
                    MemberKey = memberKey,
                    GroupKey = request.GroupKey,
                    KurinKey = Guid.NewGuid(),
                    FirstName = request.FirstName,
                    MiddleName = request.MiddleName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    DateOfBirth = request.DateOfBirth
                });

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpsertMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.Update(memberKey, request, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<MemberResponse>(ok.Value);
            Assert.Equal(memberKey, data.MemberKey);

            _mediatorMock.Verify(m => m.Send(
                It.Is<UpsertMember>(c => c.MemberKey == memberKey && c.FirstName == request.FirstName),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_ShouldReturnNotFound_WhenNotFound()
        {
            var memberKey = Guid.NewGuid();
            var request = new UpsertMemberRequest
            {
                GroupKey = Guid.NewGuid(),
                FirstName = "X",
                MiddleName = "Y",
                LastName = "Z",
                Email = "x@example.com",
                PhoneNumber = "1",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };

            var serviceResult = new ServiceResult<MemberResponse>(ResultType.NotFound);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpsertMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.Update(memberKey, request, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenInvalid()
        {
            var memberKey = Guid.NewGuid();
            var request = new UpsertMemberRequest
            {
                GroupKey = Guid.NewGuid(),
                FirstName = "Bad",
                MiddleName = "B",
                LastName = "User",
                Email = "b@example.com",
                PhoneNumber = "0",
                DateOfBirth = new DateOnly(1999, 1, 1)
            };

            var serviceResult = new ServiceResult<MemberResponse>(ResultType.BadRequest, new MemberResponse());

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpsertMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.Update(memberKey, request, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ShouldReturnOk_WhenSuccess()
        {
            var memberKey = Guid.NewGuid();
            var serviceResult = new ServiceResult<object>(ResultType.Success);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<DeleteMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.Delete(memberKey);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ShouldReturnInternalServerError_WhenUnexpected()
        {
            var memberKey = Guid.NewGuid();
            var serviceResult = new ServiceResult<object>((ResultType)999);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<DeleteMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.Delete(memberKey);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
    }
}