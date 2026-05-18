using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using robot_controller_api.Persistence;

namespace robot_controller_api.Controllers
{
    [ApiController]
    [Route("api/robot-commands")]
    public class RobotCommandsController : ControllerBase
    {
        // Repository interface injected via DI. Controller depends on abstraction, not implementation.
        private readonly IRobotCommandDataAccess _repo;

        public RobotCommandsController(IRobotCommandDataAccess repo)
        {
            _repo = repo;
        }

        // 1. GET /api/robot-commands
        // Returns all robot commands as JSON (200 OK).
        [HttpGet]
        public ActionResult<IEnumerable<RobotCommand>> GetAllRobotCommands() =>
            Ok(_repo.GetAll());

        // 2. GET /api/robot-commands/move
        // Returns only commands that are movement commands.
        [HttpGet("move")]
        public ActionResult<IEnumerable<RobotCommand>> GetMoveCommandsOnly()
        {
            var moves = _repo.GetMoveCommands();
            return Ok(moves);
        }

        // 3. GET /api/robot-commands/{id}
        // Returns a single command by id or 404 if not found.
        [HttpGet("{id}", Name = "GetRobotCommand")]
        public IActionResult GetRobotCommandById(int id)
        {
            var cmd = _repo.GetById(id);
            if (cmd == null) return NotFound();
            return Ok(cmd);
        }

        // 4. POST /api/robot-commands
        // Validates input, normalizes the name, checks for duplicates, inserts and returns 201 Created.
        [HttpPost]
        public IActionResult AddRobotCommand([FromBody] RobotCommand? newCommand)
        {
            // Basic request validation
            if (newCommand == null) return BadRequest();
            if (string.IsNullOrWhiteSpace(newCommand.Name)) return BadRequest();

            // Normalise name to a canonical form (trim + uppercase) for uniqueness checks
            var normalized = newCommand.Name.Trim().ToUpperInvariant();

            // Check for existing command with same name
            if (_repo.NameExists(normalized))
                return Conflict(new { error = "Command name already exists." });

            // Prepare entity to insert with timestamps
            var now = DateTime.UtcNow;
            var toInsert = new RobotCommand(0, normalized, newCommand.IsMoveCommand, now, now, newCommand.Description);

            try
            {
                // Insert and return CreatedAtRoute pointing to GET by id
                var created = _repo.Insert(toInsert);
                return CreatedAtRoute("GetRobotCommand", new { id = created.Id }, created);
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                // Handle unique constraint violation from the DB (defensive)
                return Conflict(new { error = "Command name already exists." });
            }
        }

        // 5. PUT /api/robot-commands/{id}
        // Validates input, ensures the resource exists, checks for name conflicts, updates and returns 204 No Content.
        [HttpPut("{id}")]
        public IActionResult UpdateRobotCommand(int id, [FromBody] RobotCommand? updatedCommand)
        {
            if (updatedCommand == null) return BadRequest();
            if (string.IsNullOrWhiteSpace(updatedCommand.Name)) return BadRequest();

            // Ensure the entity exists before attempting update
            var existing = _repo.GetById(id);
            if (existing == null) return NotFound();

            // Normalize name for uniqueness checks
            var normalized = updatedCommand.Name.Trim().ToUpperInvariant();

            // Prevent renaming to a name that already exists on another record
            if (_repo.NameExists(normalized, id))
                return Conflict(new { error = "Another command with the same name exists." });

            // Build the updated entity preserving created date and updating modified date
            var toUpdate = new RobotCommand(
                id,
                normalized,
                updatedCommand.IsMoveCommand,
                existing.CreatedDate,
                DateTime.UtcNow,
                updatedCommand.Description
            );

            try
            {
                var success = _repo.Update(id, toUpdate);
                return success ? NoContent() : NotFound();
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                // Defensive handling of DB unique constraint errors
                return Conflict(new { error = "Another command with the same name exists." });
            }
        }

        // 6. DELETE /api/robot-commands/{id}
        // Deletes the resource if it exists and returns 204 No Content on success.
        [HttpDelete("{id}")]
        public IActionResult DeleteRobotCommand(int id)
        {
            var existing = _repo.GetById(id);
            if (existing == null) return NotFound();
            var success = _repo.Delete(id);
            return success ? NoContent() : NotFound();
        }
    }
}
