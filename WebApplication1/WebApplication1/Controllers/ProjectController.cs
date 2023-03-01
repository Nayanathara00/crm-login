using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly ProjectContext _projectContext;
        public ProjectController(ProjectContext projectContext)
        {
            _projectContext = projectContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            if (_projectContext.Projects == null)
            {
                return NotFound();
            }
            return await _projectContext.Projects.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProject(int id)
        {
            if (_projectContext.Projects == null)
            {
                return NotFound();
            }
            var project = await _projectContext.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            return project;
        }

        [HttpPost]

        public async Task<ActionResult<Project>> PostProject(Project project)
        {
            _projectContext.Projects.Add(project);
            await _projectContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProject), new { id = project.ID }, project);
        }

        [HttpPut("{id}")]

        public async Task<ActionResult> PutProject(int id, Project project)
        {
            if (id != project.ID)
            {
                return BadRequest();
            }
            _projectContext.Entry(project).State = EntityState.Modified;
            try
            {
                await _projectContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProject(int id)
        {
            if (_projectContext.Projects == null)
            {
                return NotFound();
            }
            var project = await _projectContext.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            _projectContext.Projects.Remove(project);
            await _projectContext.SaveChangesAsync();

            return Ok();
        }
    }
}
