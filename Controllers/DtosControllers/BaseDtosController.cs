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
            await _context.Set<TModel>().AddAsync(model);
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateById(int id, [FromBody] TDto dto)
        {
            var model = await _context.Set<TModel>().FindAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            UpdateTheModel(model, dto);
            _context.Set<TModel>().Update(model);
            await _context.SaveChangesAsync();

            return Ok(model);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteById(int id)
        {
            var model = await _context.Set<TModel>().FindAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            _context.Set<TModel>().Remove(model);
            await _context.SaveChangesAsync();

            return Ok();
        }

        protected abstract TModel MapToModel(TDto dto);
        protected abstract void UpdateTheModel(TModel model, TDto dto);
        protected abstract int GetModelId(TModel model);
    }
}
