using MediatR;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.Feedback.UploadScreenshot;

public sealed class UploadFeedbackScreenshotCommandHandler
    : IRequestHandler<UploadFeedbackScreenshotCommand, ServiceResult<string>>
{
    private readonly IPhotoService _photoService;
    private readonly ICurrentUserContext _currentUser;

    public UploadFeedbackScreenshotCommandHandler(IPhotoService photoService, ICurrentUserContext currentUser)
    {
        _photoService = photoService;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<string>> Handle(UploadFeedbackScreenshotCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return ServiceResult<string>.Failure(ResultType.Unauthorized, "SignInRequired", "Sign in to attach a screenshot.");
        }

        try
        {
            var upload = await _photoService.UploadPhotoAsync(
                request.Content,
                request.FileName,
                BlobUploadContext.FeedbackScreenshot,
                cancellationToken);

            return new ServiceResult<string>(ResultType.Success, upload.Url);
        }
        catch (InvalidOperationException)
        {
            return ServiceResult<string>.Failure(ResultType.BadRequest, "InvalidImage", "The uploaded file is not a valid image.");
        }
    }
}
