using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using robot_controller_api.Persistence;

namespace robot_controller_api.Controllers
{
    [ApiController]
    [Route("api/maps")]
    public class MapsController : ControllerBase
    {
        // Repository interface injected via DI. Controller depends on abstraction, not concrete implementation.
        private readonly IMapDataAccess _repo;

        public MapsController(IMapDataAccess repo)
        {
            _repo = repo;
        }

        // 1. GET /api/maps
        // Returns all maps as JSON (200 OK).
        [HttpGet]
        public ActionResult<IEnumerable<Map>> GetAllMaps() =>
            Ok(_repo.GetAll());

        // 2. GET /api/maps/square
        // Returns only maps flagged as square (issquare = true).
        [HttpGet("square")]
        public ActionResult<IEnumerable<Map>> GetSquareMaps()
        {
            var squares = _repo.GetSquareMaps();
            return Ok(squares);
        }

        // 3. GET /api/maps/{id}
        // Returns a single map by id or 404 if not found.
        [HttpGet("{id}")]
        public IActionResult GetMapById(int id)
        {
            var map = _repo.GetById(id);
            if (map == null) return NotFound();
            return Ok(map);
        }

        // 4. POST /api/maps
        // Validates input, constructs a Map entity, inserts it and returns 201 Created with the new resource.
        [HttpPost]
        public IActionResult AddMap([FromBody] Map? newMap)
        {
            // Basic request validation
            if (newMap == null) return BadRequest();
            if (string.IsNullOrWhiteSpace(newMap.Name)) return BadRequest();
            if (newMap.Columns <= 0 || newMap.Rows <= 0) return BadRequest();

            // Prepare entity to insert with timestamps
            var now = DateTime.UtcNow;
            var toInsert = new Map(0, newMap.Name.Trim(), newMap.Rows, newMap.Columns, now, now, newMap.Description);

            try
            {
                // Insert and return CreatedAtAction pointing to GET by id
                var created = _repo.Insert(toInsert);
                return CreatedAtAction(nameof(GetMapById), new { id = created.Id }, created);
            }
            catch (PostgresException)
            {
                // Re-throwing here keeps behavior simple; repository may throw for constraint errors.
                throw;
            }
        }

        // 5. PUT /api/maps/{id}
        // Validates input, ensures the resource exists, updates it and returns 204 No Content on success.
        [HttpPut("{id}")]
        public IActionResult UpdateMap(int id, [FromBody] Map? updatedMap)
        {
            if (updatedMap == null) return BadRequest();
            if (string.IsNullOrWhiteSpace(updatedMap.Name)) return BadRequest();
            if (updatedMap.Columns <= 0 || updatedMap.Rows <= 0) return BadRequest();

            // Ensure the entity exists before attempting update
            var existing = _repo.GetById(id);
            if (existing == null) return NotFound();

            // Build the updated entity preserving created date and updating modified date
            var toUpdate = new Map(
                id,
                updatedMap.Name.Trim(),
                updatedMap.Rows,
                updatedMap.Columns,
                existing.CreatedDate,
                DateTime.UtcNow,
                updatedMap.Description
            );

            var success = _repo.Update(id, toUpdate);
            return success ? NoContent() : NotFound();
        }

        // 6. DELETE /api/maps/{id}
        // Deletes the resource if it exists and returns 204 No Content on success.
        [HttpDelete("{id}")]
        public IActionResult DeleteMap(int id)
        {
            var existing = _repo.GetById(id);
            if (existing == null) return NotFound();
            var success = _repo.Delete(id);
            return success ? NoContent() : NotFound();
        }

        // 7. GET /api/maps/{id}/{x}-{y}
        // Checks whether the provided coordinate (x,y) is within the bounds of the map.
        // Returns 400 for invalid coordinates, 404 if map not found, otherwise 200 with boolean result.
        [HttpGet("{id}/{x}-{y}")]
        public IActionResult CheckCoordinate(int id, int x, int y)
        {
            if (x < 0 || y < 0) return BadRequest(new { error = "Coordinates must be non-negative." });

            // Ensure the map exists before checking coordinates
            var map = _repo.GetById(id);
            if (map == null) return NotFound();

            // Delegate the bounds check to the repository
            var isOnMap = _repo.IsCoordinateOnMap(id, x, y);
            return Ok(isOnMap);
        }
    }
}
