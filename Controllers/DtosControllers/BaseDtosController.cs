using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_K.Data;
using Project_K.Models;
using Project_K.Dtos;

namespace Project_K.Controllers.DtosControllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseDtosController<TModel, TDto> : ControllerBase where TModel : class, new()
    {
        private readonly KurinDbContext _context;

        public BaseDtosController(KurinDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var models = await _context.Set<TModel>().ToListAsync();
            return Ok(models);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            var model = MapToModel(dto);
            _context.Set<TModel>().Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = GetModelId(model) }, dto);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _context.Set<TModel>().FindAsync(id);
            if (model == null)
            {
                return NotFound();
            }
            return Ok(model);
        }

        protected abstract TModel MapToModel(TDto dto);
        protected abstract int GetModelId(TModel model);
    }
}
