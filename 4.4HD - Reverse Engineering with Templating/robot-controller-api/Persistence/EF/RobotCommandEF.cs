using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace robot_controller_api.Persistence
{
    // EF implementation of IRobotCommandDataAccess
    public class RobotCommandEF : IRobotCommandDataAccess
    {
        private readonly RobotContext _context;

        public RobotCommandEF(RobotContext context)
        {
            _context = context;
        }

        public List<RobotCommand> GetAll()
        {
            return _context.RobotCommands
                .OrderBy(r => r.Id)
                .AsNoTracking()
                .ToList();
        }

        public List<RobotCommand> GetMoveCommands()
        {
            return _context.RobotCommands
                .Where(r => r.IsMoveCommand)
                .OrderBy(r => r.Id)
                .AsNoTracking()
                .ToList();
        }

        public RobotCommand? GetById(int id)
        {
            return _context.RobotCommands
                .AsNoTracking()
                .SingleOrDefault(r => r.Id == id);
        }

        public RobotCommand Insert(RobotCommand command)
        {
            // Add and save; EF will populate Id if DB returns it
            var entry = _context.RobotCommands.Add(command);
            _context.SaveChanges();
            return entry.Entity;
        }

        public bool Update(int id, RobotCommand command)
        {
            var existing = _context.RobotCommands.Find(id);
            if (existing == null) return false;

            // Update fields (preserve CreatedDate)
            existing.Name = command.Name;
            existing.Description = command.Description;
            existing.IsMoveCommand = command.IsMoveCommand;
            existing.ModifiedDate = command.ModifiedDate;

            _context.SaveChanges();
            return true;
        }

        public bool Delete(int id)
        {
            var existing = _context.RobotCommands.Find(id);
            if (existing == null) return false;

            _context.RobotCommands.Remove(existing);
            _context.SaveChanges();
            return true;
        }

        public bool NameExists(string normalizedName, int? excludeId = null)
        {
            var query = _context.RobotCommands.AsNoTracking().Where(r => r.Name == normalizedName);
            if (excludeId.HasValue) query = query.Where(r => r.Id != excludeId.Value);
            return query.Any();
        }
    }
}
