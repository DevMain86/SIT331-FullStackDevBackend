using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace robot_controller_api.Persistence
{
    // EF implementation of IMapDataAccess
    public class MapEF : IMapDataAccess
    {
        private readonly RobotContext _context;

        // Constructor: store injected DbContext
        public MapEF(RobotContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Return all maps (no tracking)
        public List<Map> GetAll()
        {
            return _context.Maps
                .OrderBy(m => m.Id)
                .AsNoTracking()
                .ToList();
        }

        // Return maps where the database has an 'issquare' flag (uses EF shadow property)
        public List<Map> GetSquareMaps()
        {
            // Fully qualify EF.Property to avoid name resolution conflicts with robot_controller_api.Persistence.EF namespace
            return _context.Maps
                .Where(m => Microsoft.EntityFrameworkCore.EF.Property<bool?>(m, "issquare") == true)
                .OrderBy(m => m.Id)
                .AsNoTracking()
                .ToList();
        }

        // Get a single map by id (no tracking)
        public Map? GetById(int id)
        {
            return _context.Maps
                .AsNoTracking()
                .SingleOrDefault(m => m.Id == id);
        }

        // Insert a new map and return the saved entity
        public Map Insert(Map map)
        {
            var entry = _context.Maps.Add(map);
            _context.SaveChanges();
            return entry.Entity;
        }

        // Update an existing map; return true if updated
        public bool Update(int id, Map map)
        {
            var existing = _context.Maps.Find(id);
            if (existing == null) return false;

            // Update CLR properties
            existing.Name = map.Name;
            existing.Description = map.Description;
            existing.Rows = map.Rows;
            existing.Columns = map.Columns;
            existing.ModifiedDate = map.ModifiedDate;

            // If a shadow property 'issquare' exists in the EF model, update it on the tracked entry
            var entry = _context.Entry(existing);
            var issquareProp = entry.Properties.FirstOrDefault(p => string.Equals(p.Metadata.Name, "issquare", StringComparison.OrdinalIgnoreCase));
            if (issquareProp != null)
            {
                issquareProp.CurrentValue = (map.Rows == map.Columns);
            }

            _context.SaveChanges();
            return true;
        }

        // Delete a map by id; return true if deleted
        public bool Delete(int id)
        {
            var existing = _context.Maps.Find(id);
            if (existing == null) return false;

            _context.Maps.Remove(existing);
            _context.SaveChanges();
            return true;
        }

        // Check whether a coordinate is within the bounds of the map
        public bool IsCoordinateOnMap(int id, int x, int y)
        {
            if (x < 0 || y < 0) return false;

            var dims = _context.Maps
                .AsNoTracking()
                .Where(m => m.Id == id)
                .Select(m => new { m.Rows, m.Columns })
                .SingleOrDefault();

            if (dims == null) return false;

            return x < dims.Columns && y < dims.Rows;
        }
    }
}