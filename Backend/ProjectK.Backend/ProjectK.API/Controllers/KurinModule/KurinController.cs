using MediatR;
using ProjectK.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Delete;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Enums;

using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Get;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Reports;
using ProjectK.Infrastructure.Reports;
using ProjectK.Common.Models.Reports;
using ProjectK.Common.Models.Dtos.KurinModule.Requests;
using ProjectK.API.Authorization;

namespace ProjectK.API.Controllers.KurinModule
{
    /// <summary>
    /// The kurin itself: its record, its badge review queue, and its report.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class KurinController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly KurinReportDataService _kurinReportDataService;
        private readonly KurinReportPdfRenderer _kurinReportPdfRenderer;

        public KurinController(
            IMediator mediator,
            KurinReportDataService kurinReportDataService,
            KurinReportPdfRenderer kurinReportPdfRenderer)
        {
            _mediator = mediator;
            _kurinReportDataService = kurinReportDataService;
            _kurinReportPdfRenderer = kurinReportPdfRenderer;
        }

        /// <summary>
        /// Lists badge submissions across the kurin that are waiting on a decision.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireGroupLeadership)]
        [HttpGet("{kurinKey:guid}/badges/review")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<BadgeProgressResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBadgeReviewQueue(Guid kurinKey)
        {
            var response = await _mediator.Send(new GetBadgeReviewQueue(kurinKey));
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Returns one kurin.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpGet("{kurinKey}")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(KurinResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByKey(Guid kurinKey)
        {
            var request = new GetKurinByKey(kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Renders the kurin's report as a PDF.
        /// </summary>
        /// <remarks>
        /// The heaviest read in the API — it walks membership, offices, probes and badges to compose one
        /// document.
        /// </remarks>
        [Authorize(Policy = AuthorizationPolicies.RequireKurinManagement)]
        [HttpGet("{kurinKey:guid}/report/pdf")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportReportPdf(Guid kurinKey, CancellationToken cancellationToken)
        {
            var report = await _kurinReportDataService.BuildAsync(kurinKey, cancellationToken);
            if (report is null)
            {
                return this.Failure(ResultType.NotFound, "KurinNotFound", "No report data exists for this kurin.");
            }

            var bytes = _kurinReportPdfRenderer.Render(report);
            var fileName = $"kurin-{report.Kurin.Number}-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";

            return File(bytes, "application/pdf", fileName);
        }

        /// <summary>
        /// Lists every kurin. Administrators only, since nobody else works above one kurin.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
        [HttpGet("kurins")]
        [ProducesResponseType(typeof(IEnumerable<KurinResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var request = new GetKurins();
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Creates a kurin from its number alone.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
        [HttpPost]
        [ProducesResponseType(typeof(KurinResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] int kurinNumber)
        {
            var request = new UpsertKurin(kurinNumber);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Rewrites a kurin's record.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpPut("{kurinKey}")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Update, "route:kurinKey")]
        [ProducesResponseType(typeof(KurinResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Upsert(Guid kurinKey, [FromBody] UpdateKurinRequest request)
        {
            var command = new UpsertKurin(
                kurinKey,
                request.Number,
                request.Stanytsia,
                request.RegionOrCountry,
                request.NamedAfter,
                request.Description,
                request.ProfileVerificationEnabled);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Deletes a kurin.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpDelete("{kurinKey}")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Delete, "route:kurinKey")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(Guid kurinKey)
        {
            var request = new DeleteKurin(kurinKey);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }
    }
}
